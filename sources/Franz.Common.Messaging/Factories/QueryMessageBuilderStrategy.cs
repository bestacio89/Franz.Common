using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;

namespace Franz.Common.Messaging.Factories;

public sealed class QueryMessageBuilderStrategy : IMessageBuilderStrategy
{
  private readonly IMessageSerializer _serializer;

  public QueryMessageBuilderStrategy()
      : this(new JsonMessageSerializer())
  {
  }

  public QueryMessageBuilderStrategy(IMessageSerializer serializer)
      => _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

  public bool CanBuild(object value) => value is IQueryMessage;

  public Message Build(object value)
  {
    ArgumentNullException.ThrowIfNull(value);
    if (!CanBuild(value))
      throw new InvalidOperationException(
          $"Type '{value.GetType().FullName}' does not implement IQuery<TResponse>.");

    var body = _serializer.Serialize(value, value.GetType());
    var message = new Message(body)
    {
      Body = body,
      Kind = MessageKind.Query
    };

    message.Headers.Add(
        MessagingConstants.ClassName,
        new[] { HeaderNamer.GetEventClassName(value.GetType()) });

    return message;
  }
}
