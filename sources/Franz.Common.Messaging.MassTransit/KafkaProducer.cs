using Confluent.Kafka;
using Franz.Common.Messaging.MassTransit.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MassTransit;
public class KafkaProducer : IKafkaProducer
{
  private readonly IProducer<string, string> _producer;

  public KafkaProducer(IProducer<string, string> producer)
  {
    _producer = producer;
  }

  public async Task PublishAsync<T>(T message, string topic, CancellationToken cancellationToken = default)
  {
    var jsonMessage = JsonSerializer.Serialize(message);
    var kafkaMessage = new Message<string, string>
    {
      Key = Guid.NewGuid().ToString(),
      Value = jsonMessage
    };

    await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
  }

  public Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException("Producer cannot subscribe.");
  }
}
