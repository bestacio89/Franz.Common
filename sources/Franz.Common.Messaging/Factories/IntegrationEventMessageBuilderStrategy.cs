using Franz.Common.Mediator;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace Franz.Common.Messaging.Factories;

public sealed class IntegrationEventMessageBuilderStrategy
    : IMessageBuilderStrategy
{
  public bool CanBuild(object value)
      => value is IIntegrationEvent;

  public Message Build(object value)
  {
    var integrationEvent = (IIntegrationEvent)value;

    var body = JsonSerializer.Serialize(
        integrationEvent,
        FranzJson.Default);

    var message = new Message(body);

    var className = HeaderNamer.GetEventClassName(value.GetType());

    message.Headers.Add(
        MessagingConstants.ClassName,
        new StringValues(className));

    return message;
  }
}
