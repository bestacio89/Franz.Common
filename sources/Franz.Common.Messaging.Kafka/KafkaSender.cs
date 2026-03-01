#nullable enable
using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Senders;

public sealed class KafkaSender : IMessagingSender, IDisposable
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
          BootstrapServers = messagingOptions.Value.BootStrapServers,
          // Best practice for Guid v7: idempotent delivery
          EnableIdempotence = true
        }).Build();

    _serializer = serializer;
    _assemblyAccessor = assemblyAccessor;
    _logger = logger;
  }

  public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new TechnicalException("Cannot send Kafka message: Message is null");

    // ✅ Topic resolution
    var assembly = _assemblyAccessor.GetEntryAssembly();
    var topicName = TopicNamer.GetTopicName(assembly);

    var jsonBody = _serializer.Serialize(message.Body ?? string.Empty);

    // BAZOOKA REFACTOR: Bridge native Guid v7 to Kafka Headers and Key
    var kafkaHeaders = new Confluent.Kafka.Headers();

    // Explicitly add the Guid v7 CorrelationId to headers for consumer re-hydration
    kafkaHeaders.Add("X-Correlation-ID", Encoding.UTF8.GetBytes(message.CorrelationId.ToString()));
    kafkaHeaders.Add("X-Message-ID", Encoding.UTF8.GetBytes(message.Id.ToString()));

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
      // USING THE GUID V7 AS KEY: 
      // This ensures all messages in a "Saga" or "Transaction" hit the same Kafka partition
      // but stay chronologically sorted thanks to the v7 timestamp prefix.
      Key = message.CorrelationId.ToString(),
      Value = jsonBody,
      Headers = kafkaHeaders
    };

    var deliveryResult = await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);

    _logger.LogInformation(
        "📤 Kafka message delivered | ID={MessageId} Corr={CorrelationId} Topic={Topic} Offset={Offset}",
        message.Id,
        message.CorrelationId,
        deliveryResult.Topic,
        deliveryResult.Offset.Value);
  }

  public void Dispose() => _producer.Dispose();
}