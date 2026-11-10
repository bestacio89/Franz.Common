using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Messages;
using Franz.Common.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.Factories;

public sealed class IntegrationEventMessageBuilderStrategy(JsonSerializerContext? jsonSerializerContext = null)
    : IMessageBuilderStrategy
{
  public bool CanBuild(object value) => value is IIntegrationEvent;

  public Message Build(object value)
  {
    ArgumentNullException.ThrowIfNull(value);
    var integrationEvent = (IIntegrationEvent)value;
    var body = Serialize(integrationEvent);
    var message = new Message(body)
    {
      Body = body,
      Kind = MessageKind.IntegrationEvent
    };

    var context = MediatorContext.Current;
    if (context.CorrelationId != Guid.Empty)
      message.CorrelationId = context.CorrelationId;
    if (context.CausationId.HasValue)
      message.Headers[Message.CausationHeader] = [context.CausationId.Value.ToString()];
    if (!string.IsNullOrWhiteSpace(context.TenantId))
      message.Headers[HeaderConstants.TenantId] = [context.TenantId];

    var className = HeaderNamer.GetEventClassName(value.GetType());
    message.Headers.Add(MessagingConstants.ClassName, [className]);
    return message;
  }

  private string Serialize(IIntegrationEvent integrationEvent)
  {
    if (jsonSerializerContext is null)
      return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), FranzJson.Default);

    var typeInfo = jsonSerializerContext.GetTypeInfo(integrationEvent.GetType())
        ?? throw new InvalidOperationException($"The JSON source-generation context does not contain metadata for '{integrationEvent.GetType()}'.");
    return JsonSerializer.Serialize(integrationEvent, typeInfo);
  }
}
