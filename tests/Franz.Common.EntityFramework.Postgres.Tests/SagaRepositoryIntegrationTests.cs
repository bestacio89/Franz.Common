using FluentAssertions;
using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.EntityFramework;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class SagaRepositoryIntegrationTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public SagaRepositoryIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task Repository_EnforcesTenantScopedPersistenceAndOptimisticConcurrency()
  {
    await using (var schemaContext = CreateContext())
      await schemaContext.Database.EnsureCreatedAsync();

    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, "tenant-a", "tenant-a");
    var tenantB = CreateRecord(sagaId, "tenant-b", "tenant-b");
    SagaPersistenceRecord createdA;

    await using (var context = CreateContext())
    {
      var repository = CreateRepository(context);
      createdA = await repository.SaveAsync(tenantA, expectedVersion: null);
      await repository.SaveAsync(tenantB, expectedVersion: null);

      var updated = await repository.SaveAsync(
          tenantA with
          {
            CreatedAt = tenantA.CreatedAt.AddDays(30),
            State = new PostgresSagaState { Value = "tenant-a-updated" }
          },
          createdA.Version);

      updated.CreatedAt.Should().Be(tenantA.CreatedAt);
      updated.Version.Should().Be(2);
    }

    await using (var restartContext = CreateContext())
    {
      var repository = CreateRepository(restartContext);
      var loadedA = await repository.LoadAsync(sagaId, "tenant-a", typeof(PostgresSagaState));
      var loadedB = await repository.LoadAsync(sagaId, "tenant-b", typeof(PostgresSagaState));
      var unscoped = await repository.LoadAsync(sagaId, null, typeof(PostgresSagaState));

      loadedA.Should().NotBeNull();
      loadedA!.CreatedAt.Should().BeCloseTo(tenantA.CreatedAt, TimeSpan.FromMilliseconds(1));
      loadedA.Version.Should().Be(2);
      ((PostgresSagaState)loadedA.State).Value.Should().Be("tenant-a-updated");
      ((PostgresSagaState)loadedB!.State).Value.Should().Be("tenant-b");
      unscoped.Should().BeNull();
    }

    var competingSaga = CreateRecord();
    await using (var createContext = CreateContext())
      await CreateRepository(createContext).SaveAsync(competingSaga, expectedVersion: null);

    await using var writerAContext = CreateContext();
    await using var writerBContext = CreateContext();
    var writerA = CreateRepository(writerAContext);
    var writerB = CreateRepository(writerBContext);

    async Task<bool> TryUpdateAsync(
        EFSagaRepository repository,
        string value)
    {
      try
      {
        await repository.SaveAsync(
            competingSaga with { State = new PostgresSagaState { Value = value } },
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

    await using (var deleteContext = CreateContext())
    {
      var repository = CreateRepository(deleteContext);
      await repository.DeleteAsync(sagaId, "tenant-a", expectedVersion: 2);
      (await repository.LoadAsync(sagaId, "tenant-a", typeof(PostgresSagaState))).Should().BeNull();
      (await repository.LoadAsync(sagaId, "tenant-b", typeof(PostgresSagaState))).Should().NotBeNull();
    }
  }

  [Fact]
  public async Task InboxSagaStateAndOutbox_CommitAndRollbackAsOneUnit()
  {
    await using (var schemaContext = CreateContext())
      await schemaContext.Database.EnsureCreatedAsync();

    var rollbackMessageId = Guid.CreateVersion7();
    var rollbackSaga = CreateRecord();
    await using (var rollbackContext = CreateContext())
    {
      IUnitOfWork unitOfWork = new EfUnitOfWork<SagaDbContext>(rollbackContext);
      var inbox = new EfSagaInboxStore(rollbackContext);
      var outbox = new EfSagaMessageStore(rollbackContext);
      var repository = CreateRepository(rollbackContext);

      await unitOfWork.BeginAsync();
      (await inbox.TryRegisterAsync(rollbackMessageId)).Should().BeTrue();
      await repository.SaveAsync(rollbackSaga, expectedVersion: null);
      await outbox.SaveAsync(new Message("{}")
      {
        MessageType = typeof(PostgresSagaState).AssemblyQualifiedName
      });
      await unitOfWork.RollbackAsync();
    }

    await using (var verifyRollbackContext = CreateContext())
    {
      var inbox = new EfSagaInboxStore(verifyRollbackContext);
      var outbox = new EfSagaMessageStore(verifyRollbackContext);
      var repository = CreateRepository(verifyRollbackContext);

      (await inbox.HasProcessedAsync(rollbackMessageId)).Should().BeFalse();
      (await repository.LoadAsync(rollbackSaga.SagaId, rollbackSaga.TenantId, typeof(PostgresSagaState)))
          .Should().BeNull();
      (await outbox.GetPendingAsync()).Should().BeEmpty();
    }

    var commitMessageId = Guid.CreateVersion7();
    var commitSaga = CreateRecord();
    await using (var commitContext = CreateContext())
    {
      IUnitOfWork unitOfWork = new EfUnitOfWork<SagaDbContext>(commitContext);
      var inbox = new EfSagaInboxStore(commitContext);
      var outbox = new EfSagaMessageStore(commitContext);
      var repository = CreateRepository(commitContext);

      await unitOfWork.BeginAsync();
      (await inbox.TryRegisterAsync(commitMessageId)).Should().BeTrue();
      await repository.SaveAsync(commitSaga, expectedVersion: null);
      await outbox.SaveAsync(new Message("{}")
      {
        MessageType = typeof(PostgresSagaState).AssemblyQualifiedName
      });
      await unitOfWork.CommitAsync();
    }

    await using (var verifyCommitContext = CreateContext())
    {
      var inbox = new EfSagaInboxStore(verifyCommitContext);
      var outbox = new EfSagaMessageStore(verifyCommitContext);
      var repository = CreateRepository(verifyCommitContext);

      (await inbox.HasProcessedAsync(commitMessageId)).Should().BeTrue();
      (await repository.LoadAsync(commitSaga.SagaId, commitSaga.TenantId, typeof(PostgresSagaState)))
          .Should().NotBeNull();
      (await outbox.GetPendingAsync()).Should().ContainSingle();
    }
  }

  [Fact]
  public async Task OutboxClaims_AreExclusiveAndExpiredLeasesCanBeReclaimed()
  {
    await using (var schemaContext = CreateContext())
    {
      await schemaContext.Database.EnsureCreatedAsync();
      await schemaContext.Database.ExecuteSqlRawAsync("DELETE FROM \"SagaOutbox\"");
    }

    var message = new Message("{}") { MessageType = "lease-contract" };
    await using (var seedContext = CreateContext())
      await new EfSagaMessageStore(seedContext).SaveAsync(message);

    await using var contextA = CreateContext();
    await using var contextB = CreateContext();
    var storeA = new EfSagaMessageStore(contextA);
    var storeB = new EfSagaMessageStore(contextB);
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
    await using var verifyContext = CreateContext();
    (await new EfSagaMessageStore(verifyContext).GetPendingAsync()).Should().BeEmpty();
  }

  [Fact]
  public async Task ScheduledOutboxMessages_AreRecoveredAfterContextRestartAndDueTime()
  {
    await using (var schemaContext = CreateContext())
    {
      await schemaContext.Database.EnsureCreatedAsync();
      await schemaContext.Database.ExecuteSqlRawAsync("DELETE FROM \"SagaOutbox\"");
    }

    var sagaId = Guid.CreateVersion7();
    var messageId = Guid.CreateVersion7();
    var notBefore = DateTimeOffset.UtcNow.AddSeconds(2);
    var record = CreateRecord(sagaId) with
    {
      Status = SagaLifecycleStatus.RetryScheduled,
      RetryCount = 1,
      LastMessageId = messageId,
      NextAttemptAt = notBefore
    };
    var message = new Message("{}")
    {
      Id = messageId,
      MessageType = "saga-retry-contract"
    };

    await using (var context = CreateContext())
    {
      var unitOfWork = new EfUnitOfWork<SagaDbContext>(context);
      await unitOfWork.BeginAsync();
      await CreateRepository(context).SaveAsync(record, expectedVersion: null);
      await new EfSagaMessageStore(context).SaveScheduledAsync(message, notBefore);
      await unitOfWork.CommitAsync();
    }

    await using (var restartedContext = CreateContext())
    {
      var persisted = await CreateRepository(restartedContext).LoadAsync(
          sagaId, record.TenantId, typeof(PostgresSagaState));
      persisted.Should().NotBeNull();
      persisted!.Status.Should().Be(SagaLifecycleStatus.RetryScheduled);
      persisted.RetryCount.Should().Be(1);
      persisted.LastMessageId.Should().Be(messageId);
      persisted.NextAttemptAt.Should().BeCloseTo(notBefore, TimeSpan.FromMilliseconds(100));

      (await new EfSagaMessageStore(restartedContext).GetPendingAsync()).Should().BeEmpty();
    }

    var remaining = notBefore - DateTimeOffset.UtcNow;
    if (remaining > TimeSpan.Zero)
      await Task.Delay(remaining + TimeSpan.FromMilliseconds(100));

    await using (var recoveredContext = CreateContext())
    {
      var recovered = await new EfSagaMessageStore(recoveredContext).GetPendingAsync();
      recovered.Should().ContainSingle().Which.Id.Should().Be(messageId);
    }
  }

  private SagaDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<SagaDbContext>()
        .UseNpgsql(_fixture.Container.GetConnectionString())
        .Options;
    return new SagaDbContext(options, Substitute.For<IDispatcher>());
  }

  private static EFSagaRepository CreateRepository(SagaDbContext context)
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
      SagaType = "postgres-contract-saga",
      State = new PostgresSagaState { Value = value },
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  public sealed class PostgresSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public string Value { get; set; } = string.Empty;
  }
}
