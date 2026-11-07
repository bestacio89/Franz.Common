using System.Collections.Concurrent;
using Franz.Common.Messaging.Sagas.Persistence;

namespace Franz.Common.Messaging.Sagas.Persistence.Memory;

/// <summary>
/// Thread-safe in-memory storage for saga records. Intended for tests and development only.
/// </summary>
public sealed class InMemorySagaStateStore
{
  public ConcurrentDictionary<Guid, SagaPersistenceRecord> Store { get; } = new();
}
