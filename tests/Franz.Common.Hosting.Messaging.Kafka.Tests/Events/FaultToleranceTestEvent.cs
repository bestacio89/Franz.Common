using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Events;

public sealed class FaultToleranceTestEvent : IIntegrationEvent
{
  public string Value { get; }

  public FaultToleranceTestEvent(string value)
  {
    Value = value;
  }
}
