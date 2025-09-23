using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging.MassTransit.Contracts;
using System.Text.Json;

namespace Franz.Common.Messaging.MassTransit;

public class KafkaConsumer : IKafkaConsumer
{
  private readonly IConsumer<string, string> _consumer;

  public KafkaConsumer(IConsumer<string, string> consumer)
  {
    _consumer = consumer;
  }

  public Task PublishAsync<T>(T message, string topic, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException("Consumer cannot publish.");
  }

  public async Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default)
  {
    _consumer.Subscribe(topic);

    while (!cancellationToken.IsCancellationRequested)
    {
      var consumeResult = _consumer.Consume(cancellationToken);

      if (string.IsNullOrWhiteSpace(consumeResult.Message?.Value))
      {
        throw new TechnicalException($"Kafka message on topic '{topic}' had no payload.");
      }

      var message = JsonSerializer.Deserialize<T>(consumeResult.Message.Value);
      if (message is null)
      {
        throw new TechnicalException($"Failed to deserialize message on topic '{topic}' into type {typeof(T).Name}.");
      }

      await handler(message);
    }
  }
}
