using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Microsoft.Extensions.Options;
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

    public async Task SendAsync<TCommandBaseRequest>(
    TCommandBaseRequest command,
    CancellationToken cancellationToken = default)
    where TCommandBaseRequest : ICommand
    {
      var message = _messageFactory.Build(command);

      var topicName = TopicNamer.GetTopicName(command.GetType().Assembly);

      // Build Kafka headers
      var kafkaHeaders = new Confluent.Kafka.Headers();
      foreach (var header in message.Headers)
      {
        var strValue = header.Value.ToString(); // StringValues → string (comma-joined if multiple)
        if (!string.IsNullOrEmpty(strValue))
        {
          kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(strValue));
        }
      }

      // Ensure body is present
      if (message.Body is null)
      {
        throw new TechnicalException(
            $"Cannot send Kafka message: Body is null for {typeof(TCommandBaseRequest).Name}");
      }

      var kafkaMessage = new Message<string, string>
      {
        Value = message.Body,
        Headers = kafkaHeaders
      };

      // Async send
      var deliveryResult = await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);

      // Optional: log result
      // _logger.LogInformation("Delivered {MessageType} to {TopicPartitionOffset}", 
      //                        typeof(TCommandBaseRequest).Name, deliveryResult.TopicPartitionOffset);
    }

  }
}


