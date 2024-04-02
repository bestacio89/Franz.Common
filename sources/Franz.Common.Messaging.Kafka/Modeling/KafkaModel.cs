using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.KafKa.Modeling;

namespace Franz.Common.Messaging.Kafka.Modeling;

public class KafkaModel : IModel
{
  private readonly IConnectionProvider _connectionProvider;
  private readonly IProducer<string, byte[]> _producer; // Use byte[] for generic message support

  public KafkaModel(IConnectionProvider connectionProvider)
  {
    _connectionProvider = connectionProvider;
    var config = new ProducerConfig
    {
      BootstrapServers = connectionProvider.Current.Name,
      // Add other producer configurations (batching, retries, acks)
    };
    _producer = new ProducerBuilder<string, byte[]>(config).Build();
  }

  public async Task Produce<TMessage>(string topic, TMessage message, CancellationToken cancel)
  {
    // Implement message serialization (e.g., JSON serialization)
    var serializedMessage = SerializeMessage(message);
    var dr = await _producer.ProduceAsync(topic, new Message<string, byte[]> { Value = serializedMessage }, cancel);
    // Handle potential production errors
  }

  private byte[] SerializeMessage<TMessage>(TMessage message)
  {
    // Implement message serialization logic using a suitable library (e.g., JSON.NET)
    // Convert the message object to a byte array representation
    throw new NotImplementedException("Message serialization not implemented");
  }

  public void Dispose()
  {
    _producer.Dispose();
  }
}
