using Franz.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;

public sealed record TestIntegrationEvent(Guid Id)
  : IIntegrationEvent;

