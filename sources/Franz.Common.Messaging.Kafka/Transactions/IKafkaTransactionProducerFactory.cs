#nullable enable
using Confluent.Kafka;

namespace Franz.Common.Messaging.Kafka.Transactions;

public interface IKafkaTransactionProducerFactory
{
  IProducer<string, byte[]> Create();
}
