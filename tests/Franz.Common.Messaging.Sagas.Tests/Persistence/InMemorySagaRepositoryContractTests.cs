using FluentAssertions;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Tests.Persistence;

public sealed class InMemorySagaRepositoryContractTests
{
  private readonly InMemorySagaRepository _repository =
      new(new InMemorySagaStateStore(), new JsonSagaStateSerializer());

  [Fact]
  public async Task SaveAsync_CreatesVersionOne_AndLoadReturnsAnIsolatedStateCopy()
  {
    var record = CreateRecord();

    var created = await _repository.SaveAsync(record, expectedVersion: null);

    created.Version.Should().Be(1);
    var loaded = await _repository.LoadAsync(record.SagaId, record.TenantId, typeof(ContractSagaState));

    loaded.Should().NotBeNull();
    loaded!.Version.Should().Be(1);
    loaded.State.Should().BeOfType<ContractSagaState>();
    ((ContractSagaState)loaded.State).Value.Should().Be("initial");
    loaded.State.Should().NotBeSameAs(record.State);
  }

  [Fact]
  public async Task SaveAsync_RejectsDuplicateCreate_AndStaleUpdate()
  {
    var record = CreateRecord();
    var created = await _repository.SaveAsync(record, expectedVersion: null);

    var duplicateCreate = () => _repository.SaveAsync(record, expectedVersion: null);
    await duplicateCreate.Should().ThrowAsync<SagaConcurrencyException>();

    var updated = record with { State = new ContractSagaState { Value = "updated" } };
    var persisted = await _repository.SaveAsync(updated, created.Version);

    persisted.Version.Should().Be(2);

    var staleUpdate = () => _repository.SaveAsync(
        record with { State = new ContractSagaState { Value = "stale" } },
        created.Version);

    await staleUpdate.Should().ThrowAsync<SagaConcurrencyException>();
  }

  [Fact]
  public async Task LoadAsync_IsolatesRecordsByTenant()
  {
    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, tenantId: "tenant-a", value: "tenant-a");
    var tenantB = CreateRecord(sagaId, tenantId: "tenant-b", value: "tenant-b");

    await _repository.SaveAsync(tenantA, expectedVersion: null);
    await _repository.SaveAsync(tenantB, expectedVersion: null);

    var loadedA = await _repository.LoadAsync(sagaId, "tenant-a", typeof(ContractSagaState));
    var loadedB = await _repository.LoadAsync(sagaId, "tenant-b", typeof(ContractSagaState));
    var unscoped = await _repository.LoadAsync(sagaId, null, typeof(ContractSagaState));

    ((ContractSagaState)loadedA!.State).Value.Should().Be("tenant-a");
    ((ContractSagaState)loadedB!.State).Value.Should().Be("tenant-b");
    unscoped.Should().BeNull();
  }

  [Fact]
  public async Task DeleteAsync_RequiresTheCurrentVersion_AndRemovesOnlyTheRequestedTenantRecord()
  {
    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, tenantId: "tenant-a", value: "tenant-a");
    var tenantB = CreateRecord(sagaId, tenantId: "tenant-b", value: "tenant-b");

    var createdA = await _repository.SaveAsync(tenantA, expectedVersion: null);
    await _repository.SaveAsync(tenantB, expectedVersion: null);

    var staleDelete = () => _repository.DeleteAsync(
        tenantA.SagaId, tenantA.TenantId, createdA.Version + 1);

    await staleDelete.Should().ThrowAsync<SagaConcurrencyException>();

    await _repository.DeleteAsync(tenantA.SagaId, tenantA.TenantId, createdA.Version);

    (await _repository.LoadAsync(tenantA.SagaId, tenantA.TenantId, typeof(ContractSagaState)))
        .Should().BeNull();
    (await _repository.LoadAsync(tenantB.SagaId, tenantB.TenantId, typeof(ContractSagaState)))
        .Should().NotBeNull();
  }

  [Fact]
  public async Task SaveAsync_AllowsOnlyOneConcurrentUpdateForAnExpectedVersion()
  {
    var record = CreateRecord();
    var created = await _repository.SaveAsync(record, expectedVersion: null);

    async Task<bool> TryUpdateAsync(string value)
    {
      try
      {
        await _repository.SaveAsync(
            record with { State = new ContractSagaState { Value = value } },
            created.Version);
        return true;
      }
      catch (SagaConcurrencyException)
      {
        return false;
      }
    }

    var outcomes = await Task.WhenAll(
        Task.Run(() => TryUpdateAsync("writer-a")),
        Task.Run(() => TryUpdateAsync("writer-b")));

    outcomes.Count(success => success).Should().Be(1);

    var persisted = await _repository.LoadAsync(
        record.SagaId, record.TenantId, typeof(ContractSagaState));

    persisted!.Version.Should().Be(2);
    new[] { "writer-a", "writer-b" }
        .Should().Contain(((ContractSagaState)persisted.State).Value);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task SaveAsync_RejectsBlankTenantIdentifiers(string tenantId)
  {
    var record = CreateRecord() with { TenantId = tenantId };

    var act = () => _repository.SaveAsync(record, expectedVersion: null);

    await act.Should().ThrowAsync<ArgumentException>()
        .WithParameterName("tenantId");
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task LoadAsync_RejectsBlankTenantIdentifiers(string tenantId)
  {
    var act = () => _repository.LoadAsync(Guid.NewGuid(), tenantId, typeof(ContractSagaState));

    await act.Should().ThrowAsync<ArgumentException>()
        .WithParameterName("tenantId");
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task DeleteAsync_RejectsBlankTenantIdentifiers(string tenantId)
  {
    var act = () => _repository.DeleteAsync(Guid.NewGuid(), tenantId, expectedVersion: 1);

    await act.Should().ThrowAsync<ArgumentException>()
        .WithParameterName("tenantId");
  }

  [Fact]
  public async Task SaveAsync_ObservesCancellationBeforeMutatingTheStore()
  {
    var record = CreateRecord();
    using var cancellation = new CancellationTokenSource();
    await cancellation.CancelAsync();

    var act = () => _repository.SaveAsync(record, expectedVersion: null, cancellation.Token);

    await act.Should().ThrowAsync<OperationCanceledException>();
    (await _repository.LoadAsync(record.SagaId, record.TenantId, typeof(ContractSagaState)))
        .Should().BeNull();
  }

  private static SagaPersistenceRecord CreateRecord(
      Guid? sagaId = null,
      string? tenantId = "tenant-a",
      string? value = "initial")
  {
    var now = DateTimeOffset.UtcNow;
    return new SagaPersistenceRecord
    {
      SagaId = sagaId ?? Guid.NewGuid(),
      TenantId = tenantId,
      SagaType = "contract-saga",
      State = new ContractSagaState { Value = value ?? string.Empty },
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  public sealed class ContractSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public string Value { get; set; } = string.Empty;
  }
}
