using Franz.Common.Messaging.Hosting.Kafka.Tests.Events;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Handlers;

public sealed class FanoutHandlerC : INotificationHandler<FanoutTestEvent2>
{
  public Task Handle(FanoutTestEvent2 notification, CancellationToken cancellationToken = default)
  {
    MultiHandlerProbe.MarkHandled();
    return Task.CompletedTask;
  }


}