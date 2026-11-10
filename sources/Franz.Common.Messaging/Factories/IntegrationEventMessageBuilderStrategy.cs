using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;
using System.Text.Json;

namespace Franz.Common.Messaging.Factories;

public sealed class IntegrationEventMessageBuilderStrategy : IMessageBuilderStrategy
{
  public bool CanBuild(object value) => value is IIntegrationEvent;

  public Message Build(object value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var integrationEvent = (IIntegrationEvent)value;
    var body = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), FranzJson.Default);

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
}
