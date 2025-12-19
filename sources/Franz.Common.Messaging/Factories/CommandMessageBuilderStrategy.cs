using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace Franz.Common.Messaging.Factories;

public sealed class CommandMessageBuilderStrategy
    : IMessageBuilderStrategy
{
  public bool CanBuild(object value)
      => value is ICommand;

  public Message Build(object value)
  {
    var command = (ICommand)value;

    var body = JsonSerializer.Serialize(
        command,
        FranzJson.Default);

    var message = new Message(body);

    var className = HeaderNamer.GetEventClassName(value.GetType());

    message.Headers.Add(
        MessagingConstants.ClassName,
        new StringValues(className));

    return message;
  }
}
