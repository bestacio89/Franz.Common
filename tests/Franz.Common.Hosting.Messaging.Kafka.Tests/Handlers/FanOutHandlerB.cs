using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

public sealed class FanoutHandlerB : IEventHandler<FanoutTestEvent>
{
  public Task HandleAsync(FanoutTestEvent evt, CancellationToken ct)
  {
    MultiHandlerProbe.Hit(evt.Value);
    return Task.CompletedTask;
  }
}
