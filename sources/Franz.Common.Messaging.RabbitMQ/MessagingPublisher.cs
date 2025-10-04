using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public class MessagingPublisher : IMessagingPublisher
{
  private readonly IModelProvider modelProvider;
  private readonly IMessagingInitializer messagingInitializer;
  private readonly IMessageFactory messageFactory;
  private readonly IMessageHandler messageHandler;
  private readonly IMessagingTransaction? messagingTransaction;

  public MessagingPublisher(
      IModelProvider modelProvider,
      IMessagingInitializer messagingInitializer,
      IMessageFactory messageFactory,
      IMessageHandler messageHandler,
      IMessagingTransaction? messagingTransaction = null)
  {
    this.modelProvider = modelProvider;
    this.messagingInitializer = messagingInitializer;
    this.messageFactory = messageFactory;
    this.messageHandler = messageHandler;
    this.messagingTransaction = messagingTransaction;
  }

  public Task Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
      where TIntegrationEvent : IIntegrationEvent
  {
    // Ensure RabbitMQ topology (exchanges/queues) exists
    messagingInitializer.Initialize();

    // Build the raw message from the integration event
    var message = messageFactory.Build(integrationEvent);

    // Apply any message pipeline handlers (headers, enrichment, etc.)
    messageHandler.Process(message);

    // Figure out which exchange to target based on the event’s assembly
    var integrationEventAssembly = integrationEvent.GetType().Assembly;
    var exchangeName = ExchangeNamer.GetEventExchangeName(integrationEventAssembly);

    // Actually publish to RabbitMQ
    PublishInternal(message, exchangeName);

    // Return completed task to respect async contract
    return Task.CompletedTask;
  }

  private void PublishInternal(Message message, string exchangeName)
  {
    var properties = modelProvider.Current.CreateBasicProperties();
    properties.Persistent = true;
    properties.Headers = message.Headers.ToDictionary(
        x => x.Key,
        x => (object)x.Value.ToString());

    var body = message.Body != null ? Encoding.UTF8.GetBytes(message.Body) : null;

    messagingTransaction?.Begin();

    modelProvider.Current.BasicPublish(
        exchange: exchangeName,
        routingKey: string.Empty,
        mandatory: false,
        basicProperties: properties,
        body: body);
  }
}
