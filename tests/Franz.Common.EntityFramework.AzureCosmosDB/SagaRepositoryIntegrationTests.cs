using System.Net.Http;
using FluentAssertions;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Cosmos;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
