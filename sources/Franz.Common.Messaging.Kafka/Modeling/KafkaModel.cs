using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.KafKa.Modeling;

namespace Franz.Common.Messaging.Kafka.Modeling;

public class KafkaModel : IModel
{
  private readonly IConnectionProvider _connectionProvider;
  private readonly IProducer<string, string> _producer;


  public KafkaModel(IConnectionProvider connectionProvider)
  {
    this._connectionProvider = connectionProvider;
    var config = new ProducerConfig { BootstrapServers = connectionProvider.Current.Name};

    
  }

  public async void Produce(string topic, string message, CancellationToken cancel)
  {
    var dr = await _producer.ProduceAsync(topic, new Message<string, string> { Value = message }, cancel);

  }

  public void Dispose()
  {
    _producer.Dispose();
  }
}
