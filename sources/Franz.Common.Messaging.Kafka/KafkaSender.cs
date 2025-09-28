using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Senders;

public class KafkaSender(
    IOptions<MessagingOptions> messagingOptions,
    IMessageSerializer serializer,
    ILogger<KafkaSender> logger)
  : IMessagingSender
{
  private readonly IProducer<string, string> _producer =
      new ProducerBuilder<string, string>(
          new ProducerConfig { BootstrapServers = messagingOptions.Value.BootStrapServers }
      ).Build();

  private readonly IMessageSerializer _serializer = serializer;
  private readonly ILogger<KafkaSender> _logger = logger;

  public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new TechnicalException("Cannot send Kafka message: Message is null");

    if (message.Body is null)
      throw new TechnicalException($"Cannot send Kafka message: Body is null for {message.MessageType}");

    var topicName = !string.IsNullOrWhiteSpace(message.MessageType)
        ? message.MessageType!
        : TopicNamer.GetTopicName(typeof(Message).Assembly);

    // Serialize the body (ensure uniform JSON representation)
    var jsonBody = _serializer.Serialize(message.Body);

    // Build Kafka headers
    var kafkaHeaders = new Confluent.Kafka.Headers();
    foreach (var header in message.Headers)
    {
      foreach (var v in header.Value)
      {
        if (!string.IsNullOrEmpty(v))
          kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(v));
      }
    }

    var kafkaMessage = new Confluent.Kafka.Message<string, string>
    {
      Key = message.CorrelationId, // partitioning key if present
      Value = jsonBody,
      Headers = kafkaHeaders
    };

    var deliveryResult = await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);

    _logger.LogInformation(
        "Delivered {MessageType} to {TopicPartitionOffset}",
        message.MessageType ?? "<unknown>",
        deliveryResult.TopicPartitionOffset);
  }
}
