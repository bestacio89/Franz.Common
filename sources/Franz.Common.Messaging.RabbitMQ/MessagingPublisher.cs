using Franz.Common.Business.Events;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using System.Text;
using RabbitMQ.Client;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class MessagingPublisher : IMessagingPublisher
{
  private readonly IModelProvider modelProvider;
  private readonly IMessagingInitializer initializer;
  private readonly IMessageFactory messageFactory;
  private readonly IMessageHandler handler;
  private readonly IMessagingTransaction? transaction;

  public MessagingPublisher(
      IModelProvider modelProvider,
      IMessagingInitializer initializer,
      IMessageFactory messageFactory,
      IMessageHandler handler,
      IMessagingTransaction? transaction = null)
  {
    this.modelProvider = modelProvider;
    this.initializer = initializer;
    this.messageFactory = messageFactory;
    this.handler = handler;
    this.transaction = transaction;
  }

  public Task Publish<TIntegrationEvent>(TIntegrationEvent evt)
      where TIntegrationEvent : IIntegrationEvent
  {
    initializer.Initialize();

    var message = messageFactory.Build(evt);
    handler.Process(message);

    var exchange = ExchangeNamer.GetEventExchangeName(evt.GetType().Assembly);

    return PublishInternalAsync(message, exchange);
  }

  private async Task PublishInternalAsync(Message message, string exchange)
  {
    var body = Encoding.UTF8.GetBytes(message.Body ?? string.Empty);

    transaction?.Begin();

    // 7.x publishing:
    await modelProvider.Current.BasicPublishAsync(
        exchange,
        routingKey: "",
        body);
  }
}
