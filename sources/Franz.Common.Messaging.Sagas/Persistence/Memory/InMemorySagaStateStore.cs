using System.Collections.Concurrent;

namespace Franz.Common.Messaging.Sagas.Persistence.Memory;

/// <summary>
/// Simple thread-safe storage for development and testing.
/// </summary>
public sealed class InMemorySagaStateStore
{
  public ConcurrentDictionary<string, string> Store { get; } = new();
}
