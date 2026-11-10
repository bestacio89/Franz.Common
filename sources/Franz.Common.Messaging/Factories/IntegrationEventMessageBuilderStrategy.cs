using System.Text.Json.Serialization;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Factories;

public sealed class IntegrationEventMessageBuilderStrategy : IMessageBuilderStrategy
{
  private readonly IMessageSerializer _serializer;

  public IntegrationEventMessageBuilderStrategy()
      : this(new JsonMessageSerializer())
  {
  }

  public IntegrationEventMessageBuilderStrategy(IMessageSerializer serializer)
      => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

  public IntegrationEventMessageBuilderStrategy(JsonSerializerContext jsonSerializerContext)
      : this(new JsonMessageSerializer(jsonSerializerContext))
  {
  }

  public bool CanBuild(object value) => value is IIntegrationEvent;

  public Message Build(object value)
  {
    ArgumentNullException.ThrowIfNull(value);
    if (value is not IIntegrationEvent)
      throw new InvalidOperationException(
          $"Type '{value.GetType().FullName}' does not implement IIntegrationEvent.");

    var body = _serializer.Serialize(value, value.GetType());
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
