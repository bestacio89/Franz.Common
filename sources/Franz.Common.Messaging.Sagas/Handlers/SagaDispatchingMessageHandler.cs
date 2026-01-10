using Franz.Common.Mediator;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Serialization;
using System.Text.Json;

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
    // 1. Resolve message type from headers
    if (!message.Headers.TryGetValue("message-type", out var typeHeader))
      return;

    var eventType = Type.GetType(typeHeader.ToString()!);
    if (eventType == null)
      return;

    // 2. Deserialize IntegrationEvent from JSON body
    var evt = (IIntegrationEvent?)_serializer.Deserialize(message.Body, eventType);
    if (evt == null)
      return;

    // 3. Extract correlation headers
    var correlationId =
        message.Headers.TryGetValue("correlation-id", out var c)
            ? c.ToString()
            : null;

    var causationId =
        message.Headers.TryGetValue("causation-id", out var cc)
            ? cc.ToString()
            : null;

    // 4. Process through SagaOrchestrator
    _orchestrator
        .HandleEventAsync(evt, correlationId, causationId, CancellationToken.None)
        .GetAwaiter()
        .GetResult();
  }
}
