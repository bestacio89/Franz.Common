using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;

public sealed class FanoutHandlerC : INotificationHandler<FanoutTestEvent2>
{
  public Task Handle(FanoutTestEvent2 notification, CancellationToken cancellationToken = default)
  {
    MultiHandlerProbe.Hit(notification.Value);
    return Task.CompletedTask;
  }


}