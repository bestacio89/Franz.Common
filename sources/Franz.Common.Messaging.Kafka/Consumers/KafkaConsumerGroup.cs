
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.KafKa.Consumers.Interfaces;

namespace Franz.Common.Messaging.KafKa.Consumers
{
  public class KafkaConsumerGroup : IConsumerGroup
  {
    private readonly IOptions<MessagingOptions> messagingOptions;
    private readonly IConsumer<Ignore, string> consumer;

    public KafkaConsumerGroup(IOptions<MessagingOptions> messagingOptions)
    {
      this.messagingOptions = messagingOptions;
      var config = new ConsumerConfig
      {
        BootstrapServers = messagingOptions.Value.BootStrapServers,
        GroupId = messagingOptions.Value.GroupID,
        AutoOffsetReset = AutoOffsetReset.Earliest
      };

      consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    }

    public void Subscribe(string topic)
    {
      consumer.Subscribe(topic);
    }

    public void Unsubscribe()
    {
      consumer.Unsubscribe();
    }

    public void Dispose()
    {
      consumer.Dispose();
    }

    public IConsumer<Ignore, string> CreateConsumer()
    {
      return consumer;
    }
  }
}
