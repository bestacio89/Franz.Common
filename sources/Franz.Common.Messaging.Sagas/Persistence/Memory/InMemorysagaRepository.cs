#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

namespace Franz.Common.Messaging.Sagas.Persistence.Memory;

/// <summary>
/// In-memory saga repository for tests and development.
/// </summary>
public sealed class InMemorySagaRepository : ISagaRepository
{
  private readonly InMemorySagaStateStore _store;
  private readonly ISagaStateSerializer _serializer;

  public InMemorySagaRepository(InMemorySagaStateStore store, ISagaStateSerializer serializer)
  {
    _store = store;
    _serializer = serializer;
  }

  public Task<object?> LoadStateAsync(
      string sagaId,
      Type stateType,
      CancellationToken cancellationToken)
  {
    if (_store.Store.TryGetValue(sagaId, out var json))
    {
      var deserialized = _serializer.Deserialize(json, stateType);
      return Task.FromResult<object?>(deserialized);
    }

    return Task.FromResult<object?>(null);
  }

  public Task SaveStateAsync(
      string sagaId,
      object state,
      CancellationToken cancellationToken)
  {
    var json = _serializer.Serialize(state);
    _store.Store[sagaId] = json;
    return Task.CompletedTask;
  }

  public Task DeleteStateAsync(
      string sagaId,
      CancellationToken cancellationToken)
  {
    _store.Store.TryRemove(sagaId, out _);
    return Task.CompletedTask;
  }
}
