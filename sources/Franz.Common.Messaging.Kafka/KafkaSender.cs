using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Senders
{
  public class KafkaSender : IMessagingSender
  {
    private readonly IProducer<string, string> _producer;
    private readonly IOptions<MessagingOptions> _messagingOptions;
    private readonly IMessageFactory _messageFactory;

    public KafkaSender(IOptions<MessagingOptions> messagingOptions, IMessageFactory messageFactory)
    {
      _messagingOptions = messagingOptions;
      _messageFactory = messageFactory;
      var config = new ProducerConfig { BootstrapServers = _messagingOptions.Value.BootStrapServers };
      _producer = new ProducerBuilder<string, string>(config).Build();
    }

    void IMessagingSender.Send<TCommandBaseRequest>(TCommandBaseRequest command)
    {
      var message = _messageFactory.Build(command);

      var topicName = TopicNamer.GetTopicName(command.GetType().Assembly);
      var headers = message.Headers.ToDictionary(x => x.Key, x => (object)x.Value.ToString());
      var body = message.Body != null ? Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(message.Body)) : null;

      _producer.Produce(topicName, new Message<string, string> { Value = body, Headers = (Confluent.Kafka.Headers)headers.Values.First() });
      _producer.Flush();
    }
  }
}


