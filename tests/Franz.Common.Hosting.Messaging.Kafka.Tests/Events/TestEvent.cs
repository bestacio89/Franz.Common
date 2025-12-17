using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Events;

using Franz.Common.Mediator.Messages;

public sealed record TestEvent(string Value) : IEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
