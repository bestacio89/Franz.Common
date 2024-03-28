using Confluent.Kafka;

namespace Franz.Common.Messaging.KafKa.Consumers.Interfaces;

public interface IConsumerGroup
{
  public IConsumer<Ignore, string> CreateConsumer();
}
