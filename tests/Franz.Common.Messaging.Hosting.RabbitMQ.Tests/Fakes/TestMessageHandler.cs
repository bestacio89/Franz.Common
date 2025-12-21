using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;

public sealed class TestMessageHandler : IMessageHandler
{
  public static Message? LastMessage;

  public void Process(Message message)
  {
    // mutate message to prove handler execution
    message.Headers["X-Test-Handled"] = "true";
    message.CorrelationId = "franz-test-correlation";

    LastMessage = message;
  }
}
