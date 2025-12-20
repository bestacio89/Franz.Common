using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
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
    var body = JsonSerializer.Serialize(value, FranzJson.Default);

    var message = new Message(body)
    {
      Kind = MessageKind.Command
    };

    message.Headers.Add(
      MessagingConstants.ClassName,
      new StringValues(HeaderNamer.GetEventClassName(value.GetType()))
    );

    return message;
  }
}

