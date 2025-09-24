namespace Franz.Common.Messaging.Kafka;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Adapters;

public static class KafkaConsumerExtensions
{
  /// <summary>
  /// Consume the next message and convert it into a mediator command.
  /// </summary>
  public static ICommand? ConsumeCommand(this KafkaConsumer consumer, TimeSpan timeout)
  {
    var result = consumer.Consume(timeout);
    if (result == null || result.Message?.Value == null)
      return null;

    var msg = System.Text.Json.JsonSerializer.Deserialize<Message>(result.Message.Value);
    return msg?.ToCommand();
  }

  /// <summary>
  /// Consume the next message and convert it into a mediator event.
  /// </summary>
  public static IEvent? ConsumeEvent(this KafkaConsumer consumer, TimeSpan timeout)
  {
    var result = consumer.Consume(timeout);
    if (result == null || result.Message?.Value == null)
      return null;

    var msg = System.Text.Json.JsonSerializer.Deserialize<Message>(result.Message.Value);
    return msg?.ToEvent();
  }
}
