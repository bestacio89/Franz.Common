#nullable enable
using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Headers;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Franz.Common.Messaging.Kafka;

public sealed class MessagingSender : IMessagingSender
{
  private readonly IProducer<string, byte[]> producer;
  private readonly IAssemblyAccessor assemblyAccessor;
  private readonly ILogger<MessagingSender> logger;

  public MessagingSender(
    IProducer<string, byte[]> producer,
    IAssemblyAccessor assemblyAccessor,
    ILogger<MessagingSender> logger)
  {
    this.producer = producer;
    this.assemblyAccessor = assemblyAccessor;
    this.logger = logger;
  }

  public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new TechnicalException("Message cannot be null");

    if (string.IsNullOrWhiteSpace(message.Body))
      throw new TechnicalException("Message body cannot be empty");

    EnsureIdentifiers(message);

    var topic = TopicNamer.GetTopicName(
      assemblyAccessor.GetEntryAssembly());

    var kafkaMessage = new Confluent.Kafka.Message<string, byte[]>
    {
      Key = message.CorrelationId,
      Value = Encoding.UTF8.GetBytes(message.Body!),
      Headers = MapToKafkaHeaders(message)
    };

    try
    {
      var result = await producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

      logger.LogInformation(
        "📤 Kafka publish succeeded | Topic={Topic} Partition={Partition} Offset={Offset}",
        result.Topic,
        result.Partition.Value,
        result.Offset.Value);
    }
    catch (ProduceException<string, byte[]> ex)
    {
      logger.LogError(ex, "🔥 Kafka publish failed for {MessageType}", message.MessageType);
      throw new TechnicalException("Kafka publish failed", ex);
    }
  }

  private static Confluent.Kafka.Headers MapToKafkaHeaders(Message message)
  {
    var kafkaHeaders = new Confluent.Kafka.Headers();

    // Franz invariants
    kafkaHeaders.Add("message-id", Encoding.UTF8.GetBytes(message.Id));
    kafkaHeaders.Add("message-type", Encoding.UTF8.GetBytes(message.MessageType ?? "unknown"));
    kafkaHeaders.Add("correlation-id", Encoding.UTF8.GetBytes(message.CorrelationId));

    // Franz MessageHeaders → Kafka headers
    foreach (var header in message.Headers)
    {
      foreach (var value in header.Value)
      {
        if (!string.IsNullOrWhiteSpace(value))
        {
          kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(value));
        }
      }
    }

    return kafkaHeaders;
  }

  private static void EnsureIdentifiers(Message message)
  {
    if (string.IsNullOrWhiteSpace(message.CorrelationId))
      message.CorrelationId = Guid.NewGuid().ToString("N");
  }
}
