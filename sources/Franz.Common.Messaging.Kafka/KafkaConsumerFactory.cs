using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;
using Microsoft.Extensions.Options;

public sealed class KafkaConsumerFactory : IKafkaConsumerFactory
{
  private readonly IOptions<MessagingOptions> _options;

  public KafkaConsumerFactory(IOptions<MessagingOptions> options)
  {
    _options = options;
  }

  public IConsumer<string, string> Build()
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = _options.Value.BootStrapServers,
      GroupId = _options.Value.GroupID,
      AutoOffsetReset = AutoOffsetReset.Earliest
    };

    return new ConsumerBuilder<string, string>(config).Build();
  }
}
