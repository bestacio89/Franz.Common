#nullable enable

using System.Collections.Generic;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Persistence.Memory;

/// <summary>
/// In-memory lifecycle repository for tests and development.
/// Uses atomic dictionary compare-and-swap operations for optimistic concurrency.
/// </summary>
public sealed class InMemorySagaRepository : ISagaRepository
{
  private readonly InMemorySagaStateStore _store;
  private readonly ISagaStateSerializer _serializer;

  public InMemorySagaRepository(InMemorySagaStateStore store, ISagaStateSerializer serializer)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
  }

  public async Task<object?> LoadStateAsync(
      Guid sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    var record = await LoadAsync(sagaId, tenantId: null, stateType, cancellationToken).ConfigureAwait(false);
    return record?.State;
  }

  public async Task SaveStateAsync(Guid sagaId, object state, CancellationToken cancellationToken)
  {
    var existing = await LoadAsync(sagaId, tenantId: null, state.GetType(), cancellationToken).ConfigureAwait(false);
    var now = DateTimeOffset.UtcNow;
    var record = new SagaPersistenceRecord
    {
      SagaId = sagaId,
      SagaType = state.GetType().AssemblyQualifiedName ?? state.GetType().FullName ?? state.GetType().Name,
      State = state,
      Status = existing?.Status ?? SagaLifecycleStatus.Running,
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
    var existing = await LoadAsync(sagaId, tenantId: null, typeof(object), cancellationToken).ConfigureAwait(false);
    if (existing is not null)
      await DeleteAsync(sagaId, tenantId: null, existing.Version, cancellationToken).ConfigureAwait(false);
  }

  public Task<SagaPersistenceRecord?> LoadAsync(
      Guid sagaId,
      string? tenantId,
      Type stateType,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stateType);
    cancellationToken.ThrowIfCancellationRequested();
    tenantId = SagaTenantId.Normalize(tenantId);

    if (!_store.Store.TryGetValue((tenantId, sagaId), out var record))
      return Task.FromResult<SagaPersistenceRecord?>(null);

    var state = _serializer.Deserialize(
        _serializer.Serialize(record.State),
        stateType == typeof(object) ? record.State.GetType() : stateType);

    return Task.FromResult<SagaPersistenceRecord?>(record with { State = state });
  }

  public Task<SagaPersistenceRecord> SaveAsync(
      SagaPersistenceRecord record,
      long? expectedVersion,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    cancellationToken.ThrowIfCancellationRequested();
    var tenantId = SagaTenantId.Normalize(record.TenantId);

    if (record.SagaId == Guid.Empty)
      throw new ArgumentException("SagaId cannot be empty.", nameof(record));

    if (record.State is not ISagaState)
      throw new ArgumentException("Saga state must implement ISagaState.", nameof(record));

    var key = (tenantId, record.SagaId);
    var persistedState = CloneState(record.State);

    if (expectedVersion is null)
    {
      var created = record with
      {
        State = persistedState,
        Version = 1,
        UpdatedAt = DateTimeOffset.UtcNow
      };

      if (!_store.Store.TryAdd(key, created))
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      return Task.FromResult(CloneRecord(created));
    }

    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!_store.Store.TryGetValue(key, out var current) ||
          current.Version != expectedVersion.Value)
        throw new SagaConcurrencyException(record.SagaId, expectedVersion);

      var updated = record with
      {
        State = persistedState,
        Version = checked(expectedVersion.Value + 1),
        CreatedAt = current.CreatedAt,
        UpdatedAt = DateTimeOffset.UtcNow
      };

      if (_store.Store.TryUpdate(key, updated, current))
        return Task.FromResult(CloneRecord(updated));
    }
  }

  public Task DeleteAsync(
      Guid sagaId,
      string? tenantId,
      long expectedVersion,
      CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    tenantId = SagaTenantId.Normalize(tenantId);
    var key = (tenantId, sagaId);

    if (!_store.Store.TryGetValue(key, out var current) ||
        current.Version != expectedVersion ||
        !((ICollection<KeyValuePair<(string? TenantId, Guid SagaId), SagaPersistenceRecord>>)_store.Store)
            .Remove(new KeyValuePair<(string? TenantId, Guid SagaId), SagaPersistenceRecord>(key, current)))
      throw new SagaConcurrencyException(sagaId, expectedVersion);

    return Task.CompletedTask;
  }

  private object CloneState(object state)
      => _serializer.Deserialize(_serializer.Serialize(state), state.GetType());

  private SagaPersistenceRecord CloneRecord(SagaPersistenceRecord record)
      => record with { State = CloneState(record.State) };
}
