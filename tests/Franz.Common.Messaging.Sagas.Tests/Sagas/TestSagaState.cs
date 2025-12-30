using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Sagas.Tests.Sagas;

public sealed class TestSagaState : ISagaState, ISagaStateWithId
{
  public string Id { get; set; } = default!;
  public int Counter { get; set; }
  public string? ConcurrencyToken { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
