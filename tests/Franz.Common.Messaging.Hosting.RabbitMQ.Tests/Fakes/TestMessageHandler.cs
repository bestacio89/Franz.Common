#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using System;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;

public sealed class TestMessageHandler : IMessageHandler
{
  public static Message? LastMessage;

  // Use a fixed Guid for testing if you need to assert against a known value
  public static readonly Guid TestCorrelationId = Guid.Parse("018e2f8a-9a91-7b3f-8e1f-4f2a3c4d5e6f");

  public void Process(Message message)
  {
    if (message is null) return;

    // ✅ FIX: "X-Test-Handled" is added to the Headers (StringValues)
    message.Headers["X-Test-Handled"] = new StringValues("true");

    // ✅ FIX: Assignment must be a Guid. 
    // We use a predefined Guid to ensure the "Spine" remains valid.
    message.CorrelationId = TestCorrelationId;

    LastMessage = message;
  }
}