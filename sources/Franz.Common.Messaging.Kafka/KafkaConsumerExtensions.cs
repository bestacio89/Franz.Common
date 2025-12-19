namespace Franz.Common.Messaging.Kafka;

using Confluent.Kafka;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Adapters;
using System.Text.Json;

public static class KafkaConsumerExtensions
{
  /// <summary>
  /// Consume the next Kafka message and convert it into a mediator command.
  /// </summary>
  public static ICommand? ConsumeCommand(
      this IConsumer<string, string> consumer,
      TimeSpan timeout)
  {
    var result = consumer.Consume(timeout);

    if (result?.Message?.Value is null)
      return null;

    var message = JsonSerializer.Deserialize<Message>(result.Message.Value);
    return message?.ToCommand();
  }

  /// <summary>
  /// Consume the next Kafka message and convert it into a mediator event.
  /// </summary>
  public static IEvent? ConsumeEvent(
      this IConsumer<string, string> consumer,
      TimeSpan timeout)
  {
    var result = consumer.Consume(timeout);

    if (result?.Message?.Value is null)
      return null;

    var message = JsonSerializer.Deserialize<Message>(result.Message.Value);
    return message?.ToEvent();
  }
}
