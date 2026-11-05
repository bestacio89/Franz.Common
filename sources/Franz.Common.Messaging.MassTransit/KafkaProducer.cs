using Confluent.Kafka;
using Franz.Common.Messaging.MassTransit.Contracts;
using System.Text.Json;

namespace Franz.Common.Messaging.MassTransit;

public class KafkaProducer : IKafkaProducer
{
  private readonly IProducer<string, string> _producer;

  public KafkaProducer(IProducer<string, string> producer)
  {
    _producer = producer ?? throw new ArgumentNullException(nameof(producer));
  }

  public async Task PublishAsync<T>(T message, string topic, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentException.ThrowIfNullOrWhiteSpace(topic);

    var kafkaMessage = new Message<string, string>
    {
      Key = Guid.NewGuid().ToString("D"),
      Value = JsonSerializer.Serialize(message)
    };

    await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken).ConfigureAwait(false);
  }
}
