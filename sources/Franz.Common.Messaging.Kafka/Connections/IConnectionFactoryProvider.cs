namespace Franz.Common.Messaging.Kafka.Connections;

using Confluent.Kafka;

public interface IConnectionFactoryProvider
{
  ProducerConfig Current { get; }
}
