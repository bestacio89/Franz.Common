using System.Net.Http;
using FluentAssertions;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Cosmos;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.CosmosDb;

namespace Franz.Common.AzureCosmosDB.Tests;

[Collection("CosmosIntegration")]
public sealed class SagaRepositoryIntegrationTests : IClassFixture<CosmosFixture>
{
  private readonly CosmosFixture _fixture;

  public SagaRepositoryIntegrationTests(CosmosFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task Repository_EnforcesTenantPartitioningAndETagConcurrency()
  {
    var databaseName = $"SagaTests_{Guid.NewGuid():N}";
    await using (var schemaContext = CreateContext(databaseName))
    {
      await schemaContext.Database.EnsureCreatedAsync();

      var entityType = schemaContext.Model.FindEntityType(typeof(CosmosSagaStateDocument));
      entityType.Should().NotBeNull();
      entityType!.FindPrimaryKey()!.Properties
          .Select(property => property.Name)
          .Should()
          .Contain(nameof(CosmosSagaStateDocument.Id))
          .And.Contain(nameof(CosmosSagaStateDocument.TenantId));
    }

    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, "tenant-a", "tenant-a");
    var tenantB = CreateRecord(sagaId, "tenant-b", "tenant-b");
    SagaPersistenceRecord createdA;

    await using (var context = CreateContext(databaseName))
    {
      var repository = CreateRepository(context);
      createdA = await repository.SaveAsync(tenantA, expectedVersion: null);
      await repository.SaveAsync(tenantB, expectedVersion: null);

      var updated = await repository.SaveAsync(
          tenantA with
          {
            CreatedAt = tenantA.CreatedAt.AddDays(30),
            State = new CosmosSagaState { Value = "tenant-a-updated" }
          },
          createdA.Version);

      updated.CreatedAt.Should().Be(tenantA.CreatedAt);
      updated.Version.Should().Be(2);
    }

    await using (var restartedContext = CreateContext(databaseName))
    {
      var repository = CreateRepository(restartedContext);
      var loadedA = await repository.LoadAsync(sagaId, "tenant-a", typeof(CosmosSagaState));
      var loadedB = await repository.LoadAsync(sagaId, "tenant-b", typeof(CosmosSagaState));
      var unscoped = await repository.LoadAsync(sagaId, null, typeof(CosmosSagaState));

      loadedA.Should().NotBeNull();
      loadedA!.CreatedAt.Should().Be(tenantA.CreatedAt);
      loadedA.Version.Should().Be(2);
      ((CosmosSagaState)loadedA.State).Value.Should().Be("tenant-a-updated");
      ((CosmosSagaState)loadedB!.State).Value.Should().Be("tenant-b");
      unscoped.Should().BeNull();
    }

    var competingSaga = CreateRecord();
    await using (var createContext = CreateContext(databaseName))
      await CreateRepository(createContext).SaveAsync(competingSaga, expectedVersion: null);

    await using var writerAContext = CreateContext(databaseName);
    await using var writerBContext = CreateContext(databaseName);
    var writerA = CreateRepository(writerAContext);
    var writerB = CreateRepository(writerBContext);

    async Task<bool> TryUpdateAsync(
        CosmosSagaRepository repository,
        string value)
    {
      try
      {
        await repository.SaveAsync(
            competingSaga with { State = new CosmosSagaState { Value = value } },
            expectedVersion: 1);
        return true;
      }
      catch (SagaConcurrencyException)
      {
        return false;
      }
    }

    var outcomes = await Task.WhenAll(
        TryUpdateAsync(writerA, "writer-a"),
        TryUpdateAsync(writerB, "writer-b"));
    outcomes.Count(x => x).Should().Be(1);

    await using var deleteContext = CreateContext(databaseName);
    var deleteRepository = CreateRepository(deleteContext);
    await deleteRepository.DeleteAsync(sagaId, "tenant-a", expectedVersion: 2);
    (await deleteRepository.LoadAsync(sagaId, "tenant-a", typeof(CosmosSagaState))).Should().BeNull();
    (await deleteRepository.LoadAsync(sagaId, "tenant-b", typeof(CosmosSagaState))).Should().NotBeNull();
  }


  [Fact]
  public async Task InboxSagaStateAndOutbox_CommitAndRollbackAsOneTransactionalBatch()
  {
    var databaseName = $"SagaAtomicity_{Guid.NewGuid():N}";
    await using (var schemaContext = CreateContext(databaseName))
      await schemaContext.Database.EnsureCreatedAsync();

    using var contextScope = Franz.Common.Mediator.Context.MediatorContext.BeginScope(
        Franz.Common.Mediator.Context.MediatorExecutionContext.Empty);

    var rollbackMessageId = Guid.CreateVersion7();
    var rollbackSaga = CreateRecord();
    await using (var rollbackContext = CreateContext(databaseName))
    {
      var unitOfWork = new CosmosSagaUnitOfWork(rollbackContext);
      var inbox = new CosmosSagaInboxStore(rollbackContext, unitOfWork);
      var outbox = new CosmosSagaMessageStore(rollbackContext, unitOfWork);
      var repository = new CosmosSagaRepository(
          rollbackContext, new JsonSagaStateSerializer(), unitOfWork);

      await unitOfWork.BeginAsync();
      (await inbox.TryRegisterAsync(rollbackMessageId)).Should().BeTrue();
      await repository.SaveAsync(rollbackSaga, expectedVersion: null);
      await outbox.SaveAsync(new Franz.Common.Messaging.Messages.Message("{}")
      {
        MessageType = typeof(CosmosSagaState).AssemblyQualifiedName
      });
      await unitOfWork.RollbackAsync();
    }

    await using (var verifyRollbackContext = CreateContext(databaseName))
    {
      var unitOfWork = new CosmosSagaUnitOfWork(verifyRollbackContext);
      var inbox = new CosmosSagaInboxStore(verifyRollbackContext, unitOfWork);
      var outbox = new CosmosSagaMessageStore(verifyRollbackContext, unitOfWork);
      var repository = CreateRepository(verifyRollbackContext);

      (await inbox.HasProcessedAsync(rollbackMessageId)).Should().BeFalse();
      (await repository.LoadAsync(rollbackSaga.SagaId, rollbackSaga.TenantId, typeof(CosmosSagaState)))
          .Should().BeNull();
      (await outbox.GetPendingAsync()).Should().BeEmpty();
    }

    var commitMessageId = Guid.CreateVersion7();
    var commitSaga = CreateRecord();
    await using (var commitContext = CreateContext(databaseName))
    {
      var unitOfWork = new CosmosSagaUnitOfWork(commitContext);
      var inbox = new CosmosSagaInboxStore(commitContext, unitOfWork);
      var outbox = new CosmosSagaMessageStore(commitContext, unitOfWork);
      var repository = new CosmosSagaRepository(
          commitContext, new JsonSagaStateSerializer(), unitOfWork);

      await unitOfWork.BeginAsync();
      (await inbox.TryRegisterAsync(commitMessageId)).Should().BeTrue();
      await repository.SaveAsync(commitSaga, expectedVersion: null);
      await outbox.SaveAsync(new Franz.Common.Messaging.Messages.Message("{}")
      {
        MessageType = typeof(CosmosSagaState).AssemblyQualifiedName
      });
      await unitOfWork.CommitAsync();
    }

    await using (var verifyCommitContext = CreateContext(databaseName))
    {
      var unitOfWork = new CosmosSagaUnitOfWork(verifyCommitContext);
      var inbox = new CosmosSagaInboxStore(verifyCommitContext, unitOfWork);
      var outbox = new CosmosSagaMessageStore(verifyCommitContext, unitOfWork);
      var repository = CreateRepository(verifyCommitContext);

      (await inbox.HasProcessedAsync(commitMessageId)).Should().BeTrue();
      (await repository.LoadAsync(commitSaga.SagaId, commitSaga.TenantId, typeof(CosmosSagaState)))
          .Should().NotBeNull();
      (await outbox.GetPendingAsync()).Should().ContainSingle();
    }
  }

  [Fact]
  public async Task AuditStore_RollsBackWithSagaTransactionAndPhysicallyDeletesExpiredRecordsInBoundedBatches()
  {
    const string tenantId = "tenant-a";
    var databaseName = $"SagaAudit_{Guid.CreateVersion7():N}";
    await using (var schemaContext = CreateContext(databaseName))
      await schemaContext.Database.EnsureCreatedAsync();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddScoped(_ => CreateContext(databaseName));
    services.AddFranzSagas(options =>
    {
      options.Persistence = "Cosmos";
      options.EnableAuditing = true;
    });

    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<CosmosSagaDbContext>();
    var container = context.Database.GetCosmosClient().GetContainer(databaseName, "SagaStates");
    var store = scope.ServiceProvider.GetRequiredService<ISagaAuditStore>();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var sagaId = Guid.CreateVersion7();

    await unitOfWork.BeginAsync();
    await store.WriteAsync(CreateAuditRecord(sagaId, tenantId, DateTimeOffset.UtcNow.AddDays(-10)));
    await unitOfWork.RollbackAsync();

    (await CountAuditDocumentsAsync(container, tenantId)).Should().Be(0L);

    await unitOfWork.BeginAsync();
    await store.WriteAsync(CreateAuditRecord(sagaId, tenantId, DateTimeOffset.UtcNow.AddDays(-10)));
    await store.WriteAsync(CreateAuditRecord(sagaId, tenantId, DateTimeOffset.UtcNow.AddDays(-20)));
    await store.WriteAsync(CreateAuditRecord(sagaId, tenantId, DateTimeOffset.UtcNow));
    await unitOfWork.CommitAsync();

    (await CountAuditDocumentsAsync(container, tenantId)).Should().Be(3L);
    (await store.DeleteExpiredAsync(DateTimeOffset.UtcNow.AddDays(-1), batchSize: 1))
        .Should().Be(1);
    (await CountAuditDocumentsAsync(container, tenantId)).Should().Be(2L);

    (await store.DeleteExpiredAsync(DateTimeOffset.UtcNow.AddDays(-1), batchSize: 1))
        .Should().Be(1);
    (await CountAuditDocumentsAsync(container, tenantId)).Should().Be(1L);
  }

  private static async Task<long> CountAuditDocumentsAsync(Container container, string tenantId)
  {
    using var iterator = container.GetItemQueryIterator<long>(
        new QueryDefinition("SELECT VALUE COUNT(1) FROM c"),
        requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId) });
    var page = await iterator.ReadNextAsync();
    return page.Single();
  }

  private static SagaAuditRecord CreateAuditRecord(
      Guid sagaId,
      string tenantId,
      DateTimeOffset timestamp)
      => new()
      {
        SagaId = sagaId,
        SagaType = "cosmos-audit-saga",
        StateType = "cosmos-audit-state",
        StepType = "Start",
        IncomingMessageType = "cosmos-audit-event",
        CorrelationId = Guid.CreateVersion7(),
        TenantId = tenantId,
        Status = SagaLifecycleStatus.Completed,
        Timestamp = timestamp
      };

  [Fact]
  public async Task OutboxClaims_UseETagConcurrencyAndRecoverExpiredLeases()
  {
    var databaseName = $"SagaOutboxLeases_{Guid.NewGuid():N}";
    await using (var schemaContext = CreateContext(databaseName))
      await schemaContext.Database.EnsureCreatedAsync();

    var message = new Franz.Common.Messaging.Messages.Message("{}")
    {
      MessageType = "lease-contract"
    };
    await using (var seedContext = CreateContext(databaseName))
    {
      var seedStore = new CosmosSagaMessageStore(
          seedContext, new CosmosSagaUnitOfWork(seedContext));
      await seedStore.SaveAsync(message);
    }

    await using var contextA = CreateContext(databaseName);
    await using var contextB = CreateContext(databaseName);
    var storeA = new CosmosSagaMessageStore(contextA, new CosmosSagaUnitOfWork(contextA));
    var storeB = new CosmosSagaMessageStore(contextB, new CosmosSagaUnitOfWork(contextB));
    var ownerA = Guid.CreateVersion7();
    var ownerB = Guid.CreateVersion7();

    var initialClaims = await Task.WhenAll(
        storeA.ClaimPendingAsync(ownerA, TimeSpan.FromSeconds(2), 1),
        storeB.ClaimPendingAsync(ownerB, TimeSpan.FromSeconds(2), 1));

    initialClaims.Sum(claim => claim.Count).Should().Be(1);
    var originalOwner = initialClaims[0].Count == 1 ? ownerA : ownerB;

    await Task.Delay(TimeSpan.FromMilliseconds(2200));

    var recoveryOwner = Guid.CreateVersion7();
    var recovered = await storeB.ClaimPendingAsync(recoveryOwner, TimeSpan.FromMinutes(1), 1);
    recovered.Should().ContainSingle().Which.Id.Should().Be(message.Id);

    var staleCompletion = () => storeA.MarkAsSentAsync(message.Id, originalOwner);
    await staleCompletion.Should().ThrowAsync<InvalidOperationException>();

    await storeB.MarkAsSentAsync(message.Id, recoveryOwner);
    await using var verifyContext = CreateContext(databaseName);
    (await new CosmosSagaMessageStore(verifyContext, new CosmosSagaUnitOfWork(verifyContext))
        .GetPendingAsync()).Should().BeEmpty();
  }

  [Fact]
  public async Task ScheduledOutboxMessages_AreRecoveredAfterContextRestartAndDueTime()
  {
    const string tenantId = "tenant-a";
    var databaseName = $"SagaRetryRecovery_{Guid.CreateVersion7():N}";
    using var tenantScope = MediatorContext.BeginScope(
        MediatorExecutionContext.Empty.WithTenant(tenantId));

    await using (var schemaContext = CreateContext(databaseName))
      await schemaContext.Database.EnsureCreatedAsync();

    var sagaId = Guid.CreateVersion7();
    var messageId = Guid.CreateVersion7();
    var notBefore = DateTimeOffset.UtcNow.AddSeconds(2);
    var record = CreateRecord(sagaId, tenantId) with
    {
      Status = SagaLifecycleStatus.RetryScheduled,
      RetryCount = 1,
      LastMessageId = messageId,
      NextAttemptAt = notBefore
    };
    var message = new Franz.Common.Messaging.Messages.Message("{}")
    {
      Id = messageId,
      MessageType = "saga-retry-contract"
    };
    message.Headers[Franz.Common.Headers.HeaderConstants.TenantId] = [tenantId];

    await using (var context = CreateContext(databaseName))
    {
      var unitOfWork = new CosmosSagaUnitOfWork(context);
      var repository = new CosmosSagaRepository(
          context, new JsonSagaStateSerializer(), unitOfWork);
      var store = new CosmosSagaMessageStore(context, unitOfWork);

      await unitOfWork.BeginAsync();
      await repository.SaveAsync(record, expectedVersion: null);
      await store.SaveScheduledAsync(message, notBefore);
      await unitOfWork.CommitAsync();
    }

    await using (var restartedContext = CreateContext(databaseName))
    {
      var persisted = await CreateRepository(restartedContext).LoadAsync(
          sagaId, tenantId, typeof(CosmosSagaState));
      persisted.Should().NotBeNull();
      persisted!.Status.Should().Be(SagaLifecycleStatus.RetryScheduled);
      persisted.RetryCount.Should().Be(1);
      persisted.LastMessageId.Should().Be(messageId);
      persisted.NextAttemptAt.Should().BeCloseTo(notBefore, TimeSpan.FromMilliseconds(100));

      var restartedStore = new CosmosSagaMessageStore(
          restartedContext, new CosmosSagaUnitOfWork(restartedContext));
      (await restartedStore.GetPendingAsync()).Should().BeEmpty();
    }

    var remaining = notBefore - DateTimeOffset.UtcNow;
    if (remaining > TimeSpan.Zero)
      await Task.Delay(remaining + TimeSpan.FromMilliseconds(100));

    await using (var recoveredContext = CreateContext(databaseName))
    {
      var recoveredStore = new CosmosSagaMessageStore(
          recoveredContext, new CosmosSagaUnitOfWork(recoveredContext));
      var recovered = await recoveredStore.GetPendingAsync();
      recovered.Should().ContainSingle().Which.Id.Should().Be(messageId);
    }
  }

  private CosmosSagaDbContext CreateContext(string databaseName)
  {
    var endpoint = $"https://localhost:{_fixture.Container.GetMappedPublicPort(8081)}/";
    var cosmosOptions = new CosmosOptions
    {
      Enabled = true,
      AccountEndpoint = endpoint,
      AccountKey = CosmosDbBuilder.DefaultAccountKey,
      DatabaseName = databaseName,
      ApplicationName = "Franz.Tests.Sagas"
    };
    var options = new DbContextOptionsBuilder<CosmosSagaDbContext>()
        .UseCosmos(
            endpoint,
            CosmosDbBuilder.DefaultAccountKey,
            databaseName,
            builder =>
            {
              builder.HttpClientFactory(() =>
              {
                var handler = new HttpClientHandler
                {
                  ServerCertificateCustomValidationCallback =
                      HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(handler);
              });
              builder.ConnectionMode(ConnectionMode.Gateway);
              builder.LimitToEndpoint();
            })
        .Options;

    return new CosmosSagaDbContext(
        options,
        Substitute.For<IDispatcher>(),
        Microsoft.Extensions.Options.Options.Create(cosmosOptions));
  }

  private static CosmosSagaRepository CreateRepository(CosmosSagaDbContext context)
      => new(context, new JsonSagaStateSerializer());

  private static SagaPersistenceRecord CreateRecord(
      Guid? sagaId = null,
      string? tenantId = "tenant-a",
      string value = "initial")
  {
    var now = DateTimeOffset.UtcNow;
    return new SagaPersistenceRecord
    {
      SagaId = sagaId ?? Guid.NewGuid(),
      TenantId = tenantId,
      SagaType = "cosmos-contract-saga",
      State = new CosmosSagaState { Value = value },
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  public sealed class CosmosSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public string Value { get; set; } = string.Empty;
  }
}
