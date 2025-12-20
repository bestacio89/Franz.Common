using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace Franz.Common.Messaging.Factories;

public sealed class QueryMessageBuilderStrategy : IMessageBuilderStrategy
{
  public bool CanBuild(object value)
  {
    if (value is null)
      return false;

    var type = value.GetType();

    return type
      .GetInterfaces()
      .Any(i =>
        i.IsGenericType &&
        i.GetGenericTypeDefinition() == typeof(IQuery<>));
  }

  public Message Build(object value)
  {
    if (!CanBuild(value))
      throw new InvalidOperationException(
        $"Type '{value.GetType().FullName}' does not implement IQuery<TResponse>.");

    // Serialize query payload
    var body = JsonSerializer.Serialize(
      value,
      FranzJson.Default);

    var message = new Message(body);

    // Logical message identity
    var className = HeaderNamer.GetEventClassName(value.GetType());

    message.Headers.Add(
      MessagingConstants.ClassName,
      new StringValues(className));

    return message;
  }
}
