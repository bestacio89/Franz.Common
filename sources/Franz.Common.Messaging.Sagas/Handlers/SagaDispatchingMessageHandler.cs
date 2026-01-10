#nullable enable
using Franz.Common.Mediator;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Sagas.Core;

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
    // 1. Resolve the .NET type name from metadata / headers
    var typeName =
      message.MessageType
      ?? (message.Headers.TryGetValue("type", out var typeValues)
            ? typeValues.ToString()
            : null);

    if (string.IsNullOrWhiteSpace(typeName))
      return; // Not a typed message, nothing for sagas

    var type = Type.GetType(typeName, throwOnError: false);
    if (type is null || !typeof(IIntegrationEvent).IsAssignableFrom(type))
      return; // Not an integration event we care about

    // 2. Deserialize the payload into an integration event instance
    if (string.IsNullOrWhiteSpace(message.Body))
      return;

    var evt = (IIntegrationEvent?)_serializer.Deserialize(message.Body!, type);
    if (evt is null)
      return;

    // 3. Correlation / causation
    var correlationId =
      !string.IsNullOrWhiteSpace(message.CorrelationId)
        ? message.CorrelationId
        : (message.Headers.TryGetValue("correlation-id", out var cid)
              ? cid.ToString()
              : null);

    var causationId =
      message.Headers.TryGetValue("causation-id", out var caid)
        ? caid.ToString()
        : null;

    // 4. Hand off to the orchestrator (sync wrapper around async)
    _orchestrator
      .HandleEventAsync(evt, correlationId, causationId, CancellationToken.None)
      .GetAwaiter()
      .GetResult();
  }
}
