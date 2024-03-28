namespace Franz.Common.Messaging.Kafka;

using Confluent.Kafka;

public interface IKafkaConsumerFactory
{
  public KafkaConsumer Build(IConsumer<string, object> consumer);
}
