using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka;

//This code is creating a Kafka consumer using the Confluent.Kafka library,
//it's using the Confluent.Kafka library classes and methods,
//and the way of consuming messages from a Kafka topic is different from AzureEventBus.
public class KafkaConsumerFactory : IKafkaConsumerFactory
{
  private readonly IOptions<MessagingOptions> _messagingOptions;

  public KafkaConsumerFactory(IOptions<MessagingOptions> messagingOptions)
  {
    _messagingOptions = messagingOptions;
  }



  public KafkaConsumer Build(IConsumer<string, object> consumer)
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = _messagingOptions.Value.BootStrapServers,
      GroupId = _messagingOptions.Value.GroupID,
      AutoOffsetReset = AutoOffsetReset.Earliest
    };

    return (KafkaConsumer)new ConsumerBuilder<string, object>(config).Build();
  }
}







