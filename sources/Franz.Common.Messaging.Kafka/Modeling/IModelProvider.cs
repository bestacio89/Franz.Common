using Confluent.Kafka;
using Franz.Common.Messaging.KafKa.Modeling;

namespace Franz.Common.Messaging.Kafka.Modeling;

public interface IModelProvider
{
  KafkaModel Current { get; }
}



