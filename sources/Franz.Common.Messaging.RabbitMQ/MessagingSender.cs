using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class MessagingSender : IMessagingSender
{
  private readonly IModelProvider modelProvider;
  private readonly IMessageHandler handler;
  private readonly IMessagingTransaction? transaction;

  public MessagingSender(
      IModelProvider modelProvider,
      IMessageHandler handler,
      IMessagingTransaction? transaction = null)
  {
    this.modelProvider = modelProvider;
    this.handler = handler;
    this.transaction = transaction;
  }

  public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    if (message.Body == null)
      throw new ArgumentNullException(nameof(message.Body));

    handler.Process(message);

    var queue = QueueNamer.GetQueueName(message.Body.GetType().Assembly);

    var bytes = Encoding.UTF8.GetBytes(message.Body);

    transaction?.Begin();

    await modelProvider.Current.BasicPublishAsync(
        exchange: "",
        routingKey: queue,
        bytes,
        cancellationToken);
  }
}
