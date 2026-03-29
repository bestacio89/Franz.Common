using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

public record ScopeTestEvent(string Id) : IEvent
{
  public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
