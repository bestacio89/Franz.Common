#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Microsoft.Extensions.Primitives;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Sagas.Handlers;

public sealed class SagaDispatchingMessageHandler : IMessageHandler
{
  private readonly SagaOrchestrator _orchestrator;
  private readonly IMessageSerializer _serializer;

  public SagaDispatchingMessageHandler(
      SagaOrchestrator orchestrator,
      IMessageSerializer serializer)
  {
    _orchestrator = orchestrator;
    _serializer = serializer;
  }

  public void Process(Message message)
  {
    // 1️⃣ Extract type name
    if (string.IsNullOrWhiteSpace(message.MessageType))
      return;

    var eventType = Type.GetType(message.MessageType);
    if (eventType == null)
      return;

    // 2️⃣ Deserialize event
    var evt = _serializer.Deserialize(message.Body, eventType) as IIntegrationEvent;
    if (evt == null)
      return;

    // 3️⃣ Extract correlation headers
    var headers = message.Headers;

    string? correlationId =
        headers.TryGetValue("correlation-id", out StringValues cid)
            ? cid.ToString()
            : null;

    string? causationId =
        headers.TryGetValue("causation-id", out StringValues ccid)
            ? ccid.ToString()
            : null;

    // 4️⃣ Dispatch to orchestrator synchronously
    _orchestrator
        .HandleEventAsync(evt, correlationId, causationId, CancellationToken.None)
        .GetAwaiter()
        .GetResult();
  }
}
