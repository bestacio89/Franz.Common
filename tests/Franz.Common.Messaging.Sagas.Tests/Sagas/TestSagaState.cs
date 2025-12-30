#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

public sealed class TestSagaState : ISagaState, ISagaStateWithId
{
  public string Id { get; set; } = string.Empty;

  public int Counter { get; set; }

  // Minimal concurrency support for tests
  public string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
}
