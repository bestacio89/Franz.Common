#nullable enable
using Confluent.Kafka;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Errors;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka
{
  public class MessagingSender : IMessagingSender
  {
    private readonly IProducer<string, byte[]> _producer;
    private readonly IMessageHandler _messageHandler;
    private readonly IMessagingTransaction _messagingTransaction;
    private readonly IMessageFactory _messageFactory;
    private readonly ILogger<MessagingSender> _logger;

    public MessagingSender(
        IProducer<string, byte[]> producer,
        IMessageHandler messageHandler,
        IMessagingTransaction messagingTransaction,
        IMessageFactory messageFactory,
        ILogger<MessagingSender> logger)
    {
      _producer = producer ?? throw new ArgumentNullException(nameof(producer));
      _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
      _messagingTransaction = messagingTransaction ?? throw new ArgumentNullException(nameof(messagingTransaction));
      _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

      if (body is null || body.Length == 0)
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

    private Confluent.Kafka.Headers BuildHeaders(Message message)
    {
      var headers = new Confluent.Kafka.Headers();

      foreach (var header in message.Headers)
      {
        // Convert StringValues to string
        var strValue = header.Value.ToString();

        if (!string.IsNullOrEmpty(strValue))
        {
          headers.Add(header.Key, Encoding.UTF8.GetBytes(strValue));
        }
      }

      return headers;
    }


    private byte[] BuildBody(Message message)
    {
      if (string.IsNullOrEmpty(message.Body))
        throw new TechnicalException("Message body cannot be null or empty.");

      return Encoding.UTF8.GetBytes(message.Body);
    }
  }
}
