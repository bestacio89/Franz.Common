using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Senders;

public class KafkaSender(
    IOptions<MessagingOptions> messagingOptions,
    IMessageFactory messageFactory,
    ILogger<KafkaSender> logger)
  : IMessagingSender
{
  private readonly IProducer<string, string> _producer =
      new ProducerBuilder<string, string>(
          new ProducerConfig { BootstrapServers = messagingOptions.Value.BootStrapServers }
      ).Build();

  private readonly IMessageFactory _messageFactory = messageFactory;
  private readonly ILogger<KafkaSender> _logger = logger;

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

    // Log delivery result
    _logger.LogInformation(
        "Delivered {MessageType} to {TopicPartitionOffset}",
        typeof(TCommandBaseRequest).Name,
        deliveryResult.TopicPartitionOffset);
  }
}
