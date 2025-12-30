#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

public sealed class TestSagaState : ISagaState, ISagaStateWithId
{
  // 🔑 Saga identity (must never be null once created)
  public string Id { get; set; } = string.Empty;

  public int Counter { get; set; }

  // 🔑 Required for concurrency control (even in-memory)
  public string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
}
