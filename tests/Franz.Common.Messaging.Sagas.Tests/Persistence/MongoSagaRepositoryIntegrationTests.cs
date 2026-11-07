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
  private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
  private IMongoDatabase _database = default!;
  private MongoSagaRepository _repository = default!;

  public async Task InitializeAsync()
  {
    await _container.StartAsync();
    var client = new MongoClient(_container.GetConnectionString());
    _database = client.GetDatabase($"franz_saga_tests_{Guid.NewGuid():N}");
    _repository = new MongoSagaRepository(_database, new JsonSagaStateSerializer());
  }

  public Task DisposeAsync() => _container.DisposeAsync().AsTask();

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
