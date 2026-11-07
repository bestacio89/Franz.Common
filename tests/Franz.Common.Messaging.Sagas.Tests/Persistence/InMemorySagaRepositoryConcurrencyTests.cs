using FluentAssertions;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Tests.Persistence;

public sealed class InMemorySagaRepositoryConcurrencyTests
{
  private readonly InMemorySagaRepository _repository = new(
      new InMemorySagaStateStore(),
      new JsonSagaStateSerializer());

  [Fact]
  public async Task SaveAsync_CreatesVersionOneRecord_WhenNoExpectedVersionIsProvided()
  {
    var sagaId = Guid.CreateVersion7();
    var state = new TestSagaState { Value = "initial" };
    var record = CreateRecord(sagaId, state);

    var saved = await _repository.SaveAsync(record, expectedVersion: null, CancellationToken.None);

    saved.SagaId.Should().Be(sagaId);
    saved.Version.Should().Be(1);
    saved.Status.Should().Be(SagaLifecycleStatus.Running);
    saved.State.Should().BeOfType<TestSagaState>()
        .Which.Value.Should().Be("initial");
  }

  [Fact]
  public async Task SaveAsync_RejectsDuplicateCreation()
  {
    var record = CreateRecord(Guid.CreateVersion7(), new TestSagaState());

    await _repository.SaveAsync(record, expectedVersion: null, CancellationToken.None);

    var act = () => _repository.SaveAsync(
        record with { State = new TestSagaState { Value = "duplicate" } },
        expectedVersion: null,
        CancellationToken.None);

    await act.Should().ThrowAsync<SagaConcurrencyException>();
  }

  [Fact]
  public async Task SaveAsync_RejectsStaleVersionWithoutOverwritingNewerState()
  {
    var sagaId = Guid.CreateVersion7();
    var initial = CreateRecord(sagaId, new TestSagaState { Value = "initial" });
    var saved = await _repository.SaveAsync(initial, expectedVersion: null, CancellationToken.None);

    var firstUpdate = saved with { State = new TestSagaState { Value = "first update" } };
    var secondUpdate = saved with { State = new TestSagaState { Value = "stale update" } };

    var updated = await _repository.SaveAsync(firstUpdate, saved.Version, CancellationToken.None);

    var act = () => _repository.SaveAsync(secondUpdate, saved.Version, CancellationToken.None);

    await act.Should().ThrowAsync<SagaConcurrencyException>();

    var loaded = await _repository.LoadAsync(sagaId, typeof(TestSagaState), CancellationToken.None);
    loaded.Should().NotBeNull();
    loaded!.Version.Should().Be(2);
    loaded.State.Should().BeOfType<TestSagaState>()
        .Which.Value.Should().Be("first update");
    updated.Version.Should().Be(2);
  }

  [Fact]
  public async Task LoadAsync_RoundTripsLifecycleMetadata()
  {
    var sagaId = Guid.CreateVersion7();
    var now = DateTimeOffset.UtcNow;
    var record = CreateRecord(sagaId, new TestSagaState())
        with
        {
          Status = SagaLifecycleStatus.Completed,
          CompletedAt = now
        };

    await _repository.SaveAsync(record, expectedVersion: null, CancellationToken.None);

    var loaded = await _repository.LoadAsync(sagaId, typeof(TestSagaState), CancellationToken.None);

    loaded.Should().NotBeNull();
    loaded!.Status.Should().Be(SagaLifecycleStatus.Completed);
    loaded.CompletedAt.Should().Be(now);
  }

  private static SagaPersistenceRecord CreateRecord(Guid sagaId, TestSagaState state)
  {
    var now = DateTimeOffset.UtcNow;

    return new SagaPersistenceRecord
    {
      SagaId = sagaId,
      SagaType = typeof(InMemorySagaRepositoryConcurrencyTests).AssemblyQualifiedName!,
      State = state,
      Status = SagaLifecycleStatus.Running,
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  public sealed class TestSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public string Value { get; set; } = string.Empty;
  }
}
