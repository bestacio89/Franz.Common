using Franz.Common.Business.Domain;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Factories;

namespace Franz.Common.Messaging.Kafka;

public sealed class MessagingPublisher : IMessagingPublisher
{
  private readonly IMessagingInitializer messagingInitializer;
  private readonly IMessageFactory messageFactory;
  private readonly IDispatcher dispatcher;
  private readonly IMessagingSender sender;

  public MessagingPublisher(
    IMessagingInitializer messagingInitializer,
    IMessageFactory messageFactory,
    IDispatcher dispatcher,
    IMessagingSender sender)
  {
    this.messagingInitializer = messagingInitializer;
    this.messageFactory = messageFactory;
    this.dispatcher = dispatcher;
    this.sender = sender;
  }

  public async Task Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
    where TIntegrationEvent : IIntegrationEvent
  {
    if (integrationEvent is null)
      throw new TechnicalException("Integration event cannot be null");

    // Ensure Kafka infra exists (topics, DLQ, etc.)
    messagingInitializer.Initialize();

    // Build Franz message
    var message = messageFactory.Build(integrationEvent);

    // Run mediator pipeline (notifications must NOT fail the system)
    await dispatcher.PublishNotificationAsync(message);

    // Delegate transport publishing
    await sender.SendAsync(message);
  }
}
