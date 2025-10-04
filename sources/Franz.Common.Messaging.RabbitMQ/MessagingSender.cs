using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public class MessagingSender : IMessagingSender
{
  private readonly IModelProvider modelProvider;
  private readonly IMessageHandler messageHandler;
  private readonly IMessagingTransaction? messagingTransaction;

  public MessagingSender(
      IModelProvider modelProvider,
      IMessageHandler messageHandler,
      IMessagingTransaction? messagingTransaction = null)
  {
    this.modelProvider = modelProvider;
    this.messageHandler = messageHandler;
    this.messagingTransaction = messagingTransaction;
  }

  public Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    // Let the message handler run any enrichment/middleware
    messageHandler.Process(message);

    // Derive target queue from the message type
    var queueName = QueueNamer.GetQueueName(message.Body.GetType().Assembly);

    var properties = BuildProperties(message);
    var body = BuildBody(message);

    messagingTransaction?.Begin();

    modelProvider.Current.BasicPublish(
        exchange: string.Empty,
        routingKey: queueName,
        mandatory: true,
        basicProperties: properties,
        body: body);

    return Task.CompletedTask;
  }

  private IBasicProperties BuildProperties(Message message)
  {
    var props = modelProvider.Current.CreateBasicProperties();
    props.Persistent = true;
    props.Headers = message.Headers.ToDictionary(
        x => x.Key,
        x => (object)x.Value.ToString() ?? string.Empty);
    return props;
  }

  private static byte[]? BuildBody(Message message)
  {
    return message.Body != null ? Encoding.UTF8.GetBytes(message.Body) : null;
  }
}
