#nullable enable
using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Delegating;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Franz.Common.Messaging.Kafka;

public class MessagingSender(
    IProducer<string, byte[]> producer,
    IMessageHandler messageHandler,
    IMessagingTransaction messagingTransaction,
    ILogger<MessagingSender> logger)
  : IMessagingSender
{
  private readonly IProducer<string, byte[]> _producer = producer;
  private readonly IMessageHandler _messageHandler = messageHandler;
  private readonly IMessagingTransaction _messagingTransaction = messagingTransaction;
  private readonly ILogger<MessagingSender> _logger = logger;

  public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new TechnicalException("Cannot send a null message.");

    if (string.IsNullOrWhiteSpace(message.Body))
      throw new TechnicalException($"Message body is null or empty for {message.MessageType ?? "<unknown>"}");

    // process pre-send hooks (optional)
    _messageHandler.Process(message);

    await SendInternalAsync(message, cancellationToken);
  }

  private async Task SendInternalAsync(Message message, CancellationToken cancellationToken)
  {
    // Resolve topic
    var topicName = !string.IsNullOrWhiteSpace(message.MessageType)
        ? message.MessageType!
        : TopicNamer.GetTopicName(typeof(Message).Assembly);

    var headers = BuildHeaders(message);
    var body = Encoding.UTF8.GetBytes(message.Body!);

    _messagingTransaction.Begin();

    try
    {
      var kafkaMessage = new Confluent.Kafka.Message<string, byte[]>
      {
        Key = message.CorrelationId, // good for partitioning
        Headers = headers,
        Value = body
      };

      var result = await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);

      _logger.LogInformation(
          "Message {MessageType} published to {Topic} [{Partition}] @ {Offset}",
          message.MessageType ?? "<unknown>",
          result.Topic,
          result.Partition.Value,
          result.Offset.Value);
    }
    catch (ProduceException<string, byte[]> ex)
    {
      _logger.LogError(ex, "Kafka publish failed for {MessageType}", message.MessageType ?? "<unknown>");
      throw new TechnicalException($"Failed to publish {message.MessageType}", ex);
    }
  }

  private static Confluent.Kafka.Headers BuildHeaders(Message message)
  {
    var headers = new Confluent.Kafka.Headers();

    foreach (var header in message.Headers)
    {
      foreach (var v in header.Value)
      {
        if (!string.IsNullOrEmpty(v))
          headers.Add(header.Key, Encoding.UTF8.GetBytes(v));
      }
    }

    return headers;
  }
}
