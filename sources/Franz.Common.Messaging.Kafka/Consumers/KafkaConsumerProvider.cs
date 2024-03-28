using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;

namespace Franz.Common.Messaging.KafKa.Consumers
{
  public class KafkaConsumerProvider 
  {
    private readonly IOptions<MessagingOptions> messagingOptions;

    public KafkaConsumerProvider(IOptions<MessagingOptions> messagingOptions)
    {
      this.messagingOptions = messagingOptions;
    }

    public IConsumer<Ignore, string> CreateConsumer()
    {
      var config = new ConsumerConfig
      {
        BootstrapServers = messagingOptions.Value.BootStrapServers,
        GroupId = messagingOptions.Value.GroupID,
        AutoOffsetReset = AutoOffsetReset.Earliest
      };

      return new ConsumerBuilder<Ignore, string>(config).Build();
    }
  }
}
