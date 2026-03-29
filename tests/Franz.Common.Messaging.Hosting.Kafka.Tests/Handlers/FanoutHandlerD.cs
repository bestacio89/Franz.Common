using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

public class FanoutHandlerD : INotificationHandler<FanoutTestEvent2>
{
  public Task Handle(FanoutTestEvent2 notification, CancellationToken cancellationToken = default)
  {
    MultiHandlerProbe.MarkHandled();
    return Task.CompletedTask;
  }
}
