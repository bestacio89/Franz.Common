using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

public sealed class FanoutHandlerB : IEventHandler<FanoutTestEvent>
{
  public Task HandleAsync(FanoutTestEvent evt, CancellationToken ct)
  {
    MultiHandlerProbe.MarkHandled();
    return Task.CompletedTask;
  }
}
