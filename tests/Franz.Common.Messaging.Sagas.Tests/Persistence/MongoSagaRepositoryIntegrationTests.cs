using Franz.Common.Messaging.Messages;
using FluentAssertions;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Mongo;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Franz.Common.Messaging.Sagas.Tests.Persistence;

public sealed class MongoSagaRepositoryIntegrationTests : IAsyncLifetime
{
  private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").WithReplicaSet().Build();
  private MongoClient _client = default!;
  private IMongoDatabase _database = default!;
  private MongoSagaRepository _repository = default!;

  public async Task InitializeAsync()
  {
    await _container.StartAsync();
    _client = new MongoClient(_container.GetConnectionString());
    _database = _client.GetDatabase($"franz_saga_tests_{Guid.NewGuid():N}");
    _repository = new MongoSagaRepository(_database, new JsonSagaStateSerializer());
  }

  public Task DisposeAsync() => _container.DisposeAsync().AsTask();

  [Fact]
  public async Task InboxSagaStateAndOutbox_CommitAndRollbackAsOneTransaction()
  {
    var rollbackMessageId = Guid.CreateVersion7();
    var rollbackSaga = CreateRecord();

    await using (var rollbackUnitOfWork = new MongoSagaUnitOfWork(_database))
    {
      var inbox = new MongoSagaInboxStore(_database, rollbackUnitOfWork);
      var outbox = new MongoSagaMessageStore(_database, rollbackUnitOfWork);
      var repository = new MongoSagaRepository(
          _database, new JsonSagaStateSerializer(), rollbackUnitOfWork);

      await rollbackUnitOfWork.BeginAsync();
      (await inbox.TryRegisterAsync(rollbackMessageId)).Should().BeTrue();
      await repository.SaveAsync(rollbackSaga, expectedVersion: null);
      await outbox.SaveAsync(new Message("{}")
      {
        MessageType = typeof(MongoSagaState).AssemblyQualifiedName
      });
      await rollbackUnitOfWork.RollbackAsync();
    }

    await using (var verificationUnitOfWork = new MongoSagaUnitOfWork(_database))
    {
      var inbox = new MongoSagaInboxStore(_database, verificationUnitOfWork);
      var outbox = new MongoSagaMessageStore(_database, verificationUnitOfWork);
      var repository = new MongoSagaRepository(_database, new JsonSagaStateSerializer());

      (await inbox.HasProcessedAsync(rollbackMessageId)).Should().BeFalse();
      (await repository.LoadAsync(
          rollbackSaga.SagaId, rollbackSaga.TenantId, typeof(MongoSagaState))).Should().BeNull();
      (await outbox.GetPendingAsync()).Should().BeEmpty();
    }

    var commitMessageId = Guid.CreateVersion7();
    var commitSaga = CreateRecord();

    await using (var commitUnitOfWork = new MongoSagaUnitOfWork(_database))
    {
      var inbox = new MongoSagaInboxStore(_database, commitUnitOfWork);
      var outbox = new MongoSagaMessageStore(_database, commitUnitOfWork);
      var repository = new MongoSagaRepository(
          _database, new JsonSagaStateSerializer(), commitUnitOfWork);

      await commitUnitOfWork.BeginAsync();
      (await inbox.TryRegisterAsync(commitMessageId)).Should().BeTrue();
      await repository.SaveAsync(commitSaga, expectedVersion: null);
      await outbox.SaveAsync(new Message("{}")
      {
        MessageType = typeof(MongoSagaState).AssemblyQualifiedName
      });
      await commitUnitOfWork.CommitAsync();
    }

    await using (var verificationUnitOfWork = new MongoSagaUnitOfWork(_database))
    {
      var inbox = new MongoSagaInboxStore(_database, verificationUnitOfWork);
      var outbox = new MongoSagaMessageStore(_database, verificationUnitOfWork);
      var repository = new MongoSagaRepository(_database, new JsonSagaStateSerializer());

      (await inbox.HasProcessedAsync(commitMessageId)).Should().BeTrue();
      (await inbox.TryRegisterAsync(commitMessageId)).Should().BeFalse();
      (await repository.LoadAsync(
          commitSaga.SagaId, commitSaga.TenantId, typeof(MongoSagaState))).Should().NotBeNull();
      (await outbox.GetPendingAsync()).Should().ContainSingle();
    }
  }

  [Fact]
  public async Task Repository_PreservesTenantScopedIdentityAndImmutableCreationMetadata()
  {
    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, "tenant-a", "tenant-a");
    var tenantB = CreateRecord(sagaId, "tenant-b", "tenant-b");

    var createdA = await _repository.SaveAsync(tenantA, expectedVersion: null);
    await _repository.SaveAsync(tenantB, expectedVersion: null);

    var updatedA = await _repository.SaveAsync(
        tenantA with
        {
          CreatedAt = tenantA.CreatedAt.AddDays(30),
          State = new MongoSagaState { Value = "tenant-a-updated" }
        },
        createdA.Version);

    updatedA.CreatedAt.Should().Be(tenantA.CreatedAt);
    updatedA.Version.Should().Be(2);

    var restartedRepository = new MongoSagaRepository(
        _database, new JsonSagaStateSerializer());
    var loadedA = await restartedRepository.LoadAsync(sagaId, "tenant-a", typeof(MongoSagaState));
    var loadedB = await restartedRepository.LoadAsync(sagaId, "tenant-b", typeof(MongoSagaState));
    var unscoped = await restartedRepository.LoadAsync(sagaId, null, typeof(MongoSagaState));

    loadedA.Should().NotBeNull();
    loadedA!.CreatedAt.Should().Be(tenantA.CreatedAt);
    loadedA.Version.Should().Be(2);
    ((MongoSagaState)loadedA.State).Value.Should().Be("tenant-a-updated");
    ((MongoSagaState)loadedB!.State).Value.Should().Be("tenant-b");
    unscoped.Should().BeNull();
  }

  [Fact]
  public async Task Repository_AllowsOnlyOneConcurrentUpdateForAnExpectedVersion()
  {
    var record = CreateRecord();
    var created = await _repository.SaveAsync(record, expectedVersion: null);

    async Task<bool> TryUpdateAsync(string value)
    {
      try
      {
        await _repository.SaveAsync(
            record with { State = new MongoSagaState { Value = value } },
            created.Version);
        return true;
      }
      catch (SagaConcurrencyException)
      {
        return false;
      }
    }

    var outcomes = await Task.WhenAll(
        TryUpdateAsync("writer-a"),
        TryUpdateAsync("writer-b"));

    outcomes.Count(x => x).Should().Be(1);
    var persisted = await _repository.LoadAsync(
        record.SagaId, record.TenantId, typeof(MongoSagaState));
    persisted!.Version.Should().Be(2);
    new[] { "writer-a", "writer-b" }
        .Should().Contain(((MongoSagaState)persisted.State).Value);
  }

  [Fact]
  public async Task Repository_DeletesOnlyTheRequestedTenantAtTheExpectedVersion()
  {
    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, "tenant-a", "tenant-a");
    var tenantB = CreateRecord(sagaId, "tenant-b", "tenant-b");
    var createdA = await _repository.SaveAsync(tenantA, expectedVersion: null);
    await _repository.SaveAsync(tenantB, expectedVersion: null);

    var staleDelete = () => _repository.DeleteAsync(sagaId, "tenant-a", createdA.Version + 1);
    await staleDelete.Should().ThrowAsync<SagaConcurrencyException>();

    await _repository.DeleteAsync(sagaId, "tenant-a", createdA.Version);

    (await _repository.LoadAsync(sagaId, "tenant-a", typeof(MongoSagaState))).Should().BeNull();
    (await _repository.LoadAsync(sagaId, "tenant-b", typeof(MongoSagaState))).Should().NotBeNull();
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task Repository_RejectsBlankTenantIdentifiers(string tenantId)
  {
    var record = CreateRecord() with { TenantId = tenantId };

    var save = () => _repository.SaveAsync(record, expectedVersion: null);
    var load = () => _repository.LoadAsync(record.SagaId, tenantId, typeof(MongoSagaState));
    var delete = () => _repository.DeleteAsync(record.SagaId, tenantId, expectedVersion: 1);

    await save.Should().ThrowAsync<ArgumentException>().WithParameterName("tenantId");
    await load.Should().ThrowAsync<ArgumentException>().WithParameterName("tenantId");
    await delete.Should().ThrowAsync<ArgumentException>().WithParameterName("tenantId");
  }

  [Fact]
  public async Task OutboxClaims_AreExclusiveAndExpiredLeasesCanBeReclaimed()
  {
    await _database.GetCollection<MongoDB.Bson.BsonDocument>("sagaOutbox")
        .DeleteManyAsync(MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Empty);

    await using var unitOfWorkA = new MongoSagaUnitOfWork(_database);
    await using var unitOfWorkB = new MongoSagaUnitOfWork(_database);
    var storeA = new MongoSagaMessageStore(_database, unitOfWorkA);
    var storeB = new MongoSagaMessageStore(_database, unitOfWorkB);
    var message = new Message("{}") { MessageType = "lease-contract" };
    await storeA.SaveAsync(message);

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
    (await storeA.GetPendingAsync()).Should().BeEmpty();
  }

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
      SagaType = "mongo-contract-saga",
      State = new MongoSagaState { Value = value },
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  public sealed class MongoSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public string Value { get; set; } = string.Empty;
  }
}
