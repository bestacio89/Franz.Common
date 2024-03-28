namespace Franz.Common.Messaging.Kafka.Connections;

using Confluent.Kafka;

public interface IConnectionProvider
{
  IProducer<string, object> Current { get; }
}




