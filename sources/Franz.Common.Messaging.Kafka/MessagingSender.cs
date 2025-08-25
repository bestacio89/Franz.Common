#nullable enable
using Confluent.Kafka;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using System.Text;

namespace Franz.Common.Messaging.Kafka
{
  public class MessagingSender : IMessagingSender
  {
    private readonly IProducer<string, byte[]> _producer;
    private readonly IMessageHandler _messageHandler;
    private readonly IMessagingTransaction _messagingTransaction;
    private readonly IMessageFactory _messageFactory;

    
      public MessagingSender(
        IProducer<string, byte[]> producer,
        IMessageHandler messageHandler,
        IMessagingTransaction messagingTransaction,
        IMessageFactory messageFactory)
    {
      _producer = producer;
      _messageHandler = messageHandler;
      _messagingTransaction = messagingTransaction;
      _messageFactory = messageFactory;
    }

    public void Send<TCommandBaseRequest>(TCommandBaseRequest command) where TCommandBaseRequest : ICommand
    {
      var message = _messageFactory.Build(command);

      _messageHandler.Process(message);

      Send(command, message);
    }

    private void Send(ICommand command, Message message)
    {
      if (_producer is null)
        throw new InvalidOperationException("Kafka producer is not initialized.");

      var topicName = TopicNamer.GetTopicName(command.GetType().Assembly);
      var headers = BuildHeaders(message) ?? new Confluent.Kafka.Headers(); // fallback to empty headers
      var body = BuildBody(message) ?? Array.Empty<byte>();                 // fallback to empty body

      _messagingTransaction?.Begin();

      // ProduceAsync returns a Task, we should await it if this method becomes async
      _producer.ProduceAsync(topicName, new Confluent.Kafka.Message<string, byte[]>
      {
        Headers = headers,
        Value = body
      });

      _producer.Flush();
    }

    private Confluent.Kafka.Headers BuildHeaders(Message message)
    {
      var headers = new Confluent.Kafka.Headers();
      foreach (var header in message.Headers)
      {
        headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString()));
      }
      return headers;
    }

    private byte[]? BuildBody(Message message)
    {
      return message.Body != null ? Encoding.UTF8.GetBytes(message.Body) : null;
    }
  }
}
