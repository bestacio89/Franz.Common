using Confluent.Kafka;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Kafka.Modeling;
using System.Text;

namespace Franz.Common.Messaging.Kafka;

public class MessagingPublisher : IMessagingPublisher
{
  private readonly IProducer<string, byte[]> _producer;
  private readonly IMessagingInitializer _messagingInitializer;
  private readonly IMessageFactory _messageFactory;
  private readonly IDispatcher _dispatcher; // Franz mediator dispatcher

  public MessagingPublisher(
      IProducer<string, byte[]> producer,
      IMessagingInitializer messagingInitializer,
      IMessageFactory messageFactory,
      IDispatcher dispatcher)
  {
    _producer = producer;
    _messagingInitializer = messagingInitializer;
    _messageFactory = messageFactory;
    _dispatcher = dispatcher;
  }

  public async Task Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
      where TIntegrationEvent : BaseDomainEvent, IIntegrationEvent
  {
    // Ensure messaging infra is ready
    _messagingInitializer.Initialize();

    // Build message with Franz factory
    var message = _messageFactory.Build(integrationEvent);

    // Run through Franz mediator pipeline (instead of IMessageHandler / MediatR)
    await _dispatcher.Send(message);

    // Resolve Kafka topic
    var integrationEventAssembly = integrationEvent.GetType().Assembly;
    var topic = TopicNamer.GetTopicName(integrationEventAssembly);

    // Map headers into Confluent format
    var confluentHeaders = new Confluent.Kafka.Headers();
    foreach (var header in message.Headers)
    {
      confluentHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString()!));
    }

    // Send to Kafka
    try
    {
      var deliveryResult = await _producer.ProduceAsync(topic, new Message<string, byte[]>
      {
        Headers = confluentHeaders,
        Value = Encoding.UTF8.GetBytes(message.Body)
      });

      _producer.Flush();
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to publish event to Kafka", ex);
    }
  }
}
