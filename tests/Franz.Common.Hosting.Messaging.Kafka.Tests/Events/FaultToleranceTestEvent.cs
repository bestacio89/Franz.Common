using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Events;

public sealed record FaultToleranceTestEvent(string Value) : IEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

