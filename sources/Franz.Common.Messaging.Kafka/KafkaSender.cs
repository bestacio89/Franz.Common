using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Senders;

public sealed class KafkaSender : IMessagingSender
{
  private readonly IProducer<string, string> _producer;
  private readonly IMessageSerializer _serializer;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly ILogger<KafkaSender> _logger;

  public KafkaSender(
      IOptions<MessagingOptions> messagingOptions,
      IMessageSerializer serializer,
      IAssemblyAccessor assemblyAccessor,
      ILogger<KafkaSender> logger)
  {
    _producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
          BootstrapServers = messagingOptions.Value.BootStrapServers
        })
      .Build();

    _serializer = serializer;
    _assemblyAccessor = assemblyAccessor;
    _logger = logger;
  }

  public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new TechnicalException("Cannot send Kafka message: Message is null");

    if (message.Body is null)
      throw new TechnicalException(
        $"Cannot send Kafka message: Body is null for {message.MessageType}");

    // ✅ Franz-approved topic resolution
    var assembly = _assemblyAccessor.GetEntryAssembly();
    var topicName = TopicNamer.GetTopicName(assembly);

    var jsonBody = _serializer.Serialize(message.Body);

    var kafkaHeaders = new Confluent.Kafka.Headers();
    foreach (var header in message.Headers)
    {
      foreach (var v in header.Value)
      {
        if (!string.IsNullOrWhiteSpace(v))
          kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(v));
      }
    }

    var kafkaMessage = new Confluent.Kafka.Message<string, string>
    {
      Key = message.CorrelationId,
      Value = jsonBody,
      Headers = kafkaHeaders
    };

    var deliveryResult =
      await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);

    _logger.LogInformation(
      "📤 Kafka message delivered | Type={MessageType} Topic={Topic} Offset={Offset}",
      message.MessageType ?? "<unknown>",
      deliveryResult.Topic,
      deliveryResult.Offset.Value);
  }
}
