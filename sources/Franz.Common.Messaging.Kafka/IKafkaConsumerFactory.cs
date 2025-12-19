namespace Franz.Common.Messaging.Kafka;

using Confluent.Kafka;

public interface IKafkaConsumerFactory
{
  public IConsumer<string, string> Build();
}
