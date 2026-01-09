#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

public sealed class TestSagaState : ISagaState, ISagaStateWithId
{
  public string Id { get; set; } = string.Empty;

  public int Counter { get; set; }

  // Even memory stores should have a concurrency token
  public string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
}
