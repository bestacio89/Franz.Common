#nullable enable
using Confluent.Kafka;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Errors;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Franz.Common.Messaging.Kafka;

public class MessagingSender(
    IProducer<string, byte[]> producer,
    IMessageHandler messageHandler,
    IMessagingTransaction messagingTransaction,
    IMessageFactory messageFactory,
    ILogger<MessagingSender> logger)
  : IMessagingSender
{
  private readonly IProducer<string, byte[]> _producer = producer;
  private readonly IMessageHandler _messageHandler = messageHandler;
  private readonly IMessagingTransaction _messagingTransaction = messagingTransaction;
  private readonly IMessageFactory _messageFactory = messageFactory;
  private readonly ILogger<MessagingSender> _logger = logger;

  public async Task SendAsync<TCommandBaseRequest>(TCommandBaseRequest command, CancellationToken cancellationToken = default)
      where TCommandBaseRequest : ICommand
  {
    if (command is null)
      throw new TechnicalException("Cannot send a null command.");

    var message = _messageFactory.Build(command);

    _messageHandler.Process(message);

    await SendInternalAsync(command, message, cancellationToken);
  }

  private async Task SendInternalAsync(ICommand command, Message message, CancellationToken cancellationToken)
  {
    var topicName = TopicNamer.GetTopicName(command.GetType().Assembly);
    var headers = BuildHeaders(message);
    var body = BuildBody(message);

    if (body.Length == 0)
      throw new TechnicalException($"Message body for {command.GetType().Name} is empty.");

    _messagingTransaction.Begin();

    try
    {
      var kafkaMessage = new Confluent.Kafka.Message<string, byte[]>
      {
        Headers = headers,
        Value = body
      };

      var result = await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);

      _logger.LogInformation(
          "Message published to {Topic} [{Partition}] @ {Offset}",
          result.Topic,
          result.Partition.Value,
          result.Offset.Value);
    }
    catch (ProduceException<string, byte[]> ex)
    {
      _logger.LogError(ex, "Kafka publish failed for {Command}", command.GetType().Name);
      throw new TechnicalException($"Failed to publish {command.GetType().Name}", ex);
    }
  }

  private static Confluent.Kafka.Headers BuildHeaders(Message message)
  {
    var headers = new Confluent.Kafka.Headers();

    foreach (var header in message.Headers)
    {
      var strValue = header.Value.ToString();
      if (!string.IsNullOrEmpty(strValue))
      {
        headers.Add(header.Key, Encoding.UTF8.GetBytes(strValue));
      }
    }

    return headers;
  }

  private static byte[] BuildBody(Message message)
  {
    if (string.IsNullOrEmpty(message.Body))
      throw new TechnicalException("Message body cannot be null or empty.");

    return Encoding.UTF8.GetBytes(message.Body);
  }
}
