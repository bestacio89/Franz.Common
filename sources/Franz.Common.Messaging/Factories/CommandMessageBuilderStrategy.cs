using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Factories;

public sealed class CommandMessageBuilderStrategy : IMessageBuilderStrategy
{
  private readonly IMessageSerializer _serializer;

  public CommandMessageBuilderStrategy()
      : this(new JsonMessageSerializer())
  {
  }

  public CommandMessageBuilderStrategy(IMessageSerializer serializer)
      => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

  public bool CanBuild(object value) => value is ICommand;

  public Message Build(object value)
  {
    ArgumentNullException.ThrowIfNull(value);
    if (!CanBuild(value))
      throw new InvalidOperationException($"Type '{value.GetType().FullName}' does not implement ICommand.");

    var body = _serializer.Serialize(value, value.GetType());
    var message = new Message(body)
    {
      Body = body,
      Kind = MessageKind.Command
    };

    message.Headers.Add(
        MessagingConstants.ClassName,
        new[] { HeaderNamer.GetEventClassName(value.GetType()) });

    return message;
  }
}
