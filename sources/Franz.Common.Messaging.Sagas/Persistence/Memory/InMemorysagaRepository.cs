#nullable enable

using System.Collections.Generic;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Persistence.Memory;

/// <summary>
/// In-memory lifecycle repository for tests and development.
/// Uses atomic dictionary compare-and-swap operations for optimistic concurrency.
/// </summary>
public sealed class InMemorySagaRepository : ISagaRepository, ISagaLifecycleRepository
{
  private readonly InMemorySagaStateStore _store;
  private readonly ISagaStateSerializer _serializer;

  public InMemorySagaRepository(InMemorySagaStateStore store, ISagaStateSerializer serializer)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
  }

  public Task<object?> LoadStateAsync(
      Guid sagaId,
      Type stateType,
      CancellationToken cancellationToken)
      => LoadLegacyStateAsync(sagaId, stateType, cancellationToken);

  private async Task<object?> LoadLegacyStateAsync(
      Guid sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    var record = await LoadAsync(sagaId, stateType, cancellationToken).ConfigureAwait(false);
    return record?.State;
  }

  public async Task SaveStateAsync(
      Guid sagaId,
      object state,
      CancellationToken cancellationToken)
  {
    var existing = await LoadAsync(sagaId, state.GetType(), cancellationToken).ConfigureAwait(false);
    var now = DateTimeOffset.UtcNow;
    var record = new SagaPersistenceRecord
    {
      SagaId = sagaId,
      SagaType = state.GetType().AssemblyQualifiedName ?? state.GetType().FullName ?? state.GetType().Name,
      State = state,
      Status = existing?.Status ?? Franz.Common.Messaging.Sagas.Abstractions.SagaLifecycleStatus.Running,
      CreatedAt = existing?.CreatedAt ?? now,
      UpdatedAt = now,
      CompletedAt = existing?.CompletedAt,
      FailedAt = existing?.FailedAt,
      NextAttemptAt = existing?.NextAttemptAt,
      LastMessageId = existing?.LastMessageId,
      LastError = existing?.LastError
    };

    await SaveAsync(record, existing?.Version, cancellationToken).ConfigureAwait(false);
  }

  public async Task DeleteStateAsync(Guid sagaId, CancellationToken cancellationToken)
  {
    if (_store.Store.TryGetValue(sagaId, out var existing))
      await DeleteAsync(sagaId, existing.Version, cancellationToken).ConfigureAwait(false);
  }

  public Task<SagaPersistenceRecord?> LoadAsync(
      Guid sagaId,
      Type stateType,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stateType);
    cancellationToken.ThrowIfCancellationRequested();

    if (!_store.Store.TryGetValue(sagaId, out var record))
      return Task.FromResult<SagaPersistenceRecord?>(null);

    var state = _serializer.Deserialize(
        _serializer.Serialize(record.State),
        stateType);

    return Task.FromResult<SagaPersistenceRecord?>(record with { State = state });
  }

  public Task<SagaPersistenceRecord> SaveAsync(
      SagaPersistenceRecord record,
      long? expectedVersion,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    cancellationToken.ThrowIfCancellationRequested();

    if (record.SagaId == Guid.Empty)
      throw new ArgumentException("SagaId cannot be empty.", nameof(record));

    if (record.State is not Franz.Common.Messaging.Sagas.Abstractions.ISagaState)
      throw new ArgumentException("Saga state must implement ISagaState.", nameof(record));

    var persistedState = _serializer.Deserialize(
        _serializer.Serialize(record.State),
        record.State.GetType());

    if (expectedVersion is null)
    {
      var created = record with
      {
        State = persistedState,
        Version = 1,
        UpdatedAt = DateTimeOffset.UtcNow
      };

      if (!_store.Store.TryAdd(record.SagaId, created))
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      return Task.FromResult(CloneRecord(created));
    }

    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!_store.Store.TryGetValue(record.SagaId, out var current) ||
          current.Version != expectedVersion.Value)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      var updated = record with
      {
        State = persistedState,
        Version = checked(expectedVersion.Value + 1),
        CreatedAt = current.CreatedAt,
        UpdatedAt = DateTimeOffset.UtcNow
      };

      if (_store.Store.TryUpdate(record.SagaId, updated, current))
        return Task.FromResult(CloneRecord(updated));
    }
  }

  public Task DeleteAsync(
      Guid sagaId,
      long expectedVersion,
      CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!_store.Store.TryGetValue(sagaId, out var current) ||
        current.Version != expectedVersion ||
        !((ICollection<KeyValuePair<Guid, SagaPersistenceRecord>>)_store.Store)
            .Remove(new KeyValuePair<Guid, SagaPersistenceRecord>(sagaId, current)))
      throw new SagaConcurrencyException(sagaId, expectedVersion);

    return Task.CompletedTask;
  }

  private SagaPersistenceRecord CloneRecord(SagaPersistenceRecord record)
      => record with
      {
        State = _serializer.Deserialize(
            _serializer.Serialize(record.State),
            record.State.GetType())
      };
}
