using Confluent.Kafka;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Kafka.Modeling;
using System.Text;
using Franz.Common.Errors; // Assuming TechnicalException lives here

namespace Franz.Common.Messaging.Kafka;

public class MessagingPublisher : IMessagingPublisher
{
  private readonly IProducer<string, byte[]> _producer;
  private readonly IMessagingInitializer _messagingInitializer;
  private readonly IMessageFactory _messageFactory;
  private readonly IDispatcher _dispatcher;

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

    // Run through Franz mediator pipeline
    await _dispatcher.PublishAsync(message);

    // Resolve Kafka topic
    var integrationEventAssembly = integrationEvent.GetType().Assembly;
    var topic = TopicNamer.GetTopicName(integrationEventAssembly);

    // Build Kafka headers
    var confluentheaders = new Confluent.Kafka.Headers();
    foreach (var header in message.Headers)
    {
      var strValue = header.Value.ToString();
      if (!string.IsNullOrEmpty(strValue))
      {
        confluentheaders.Add(header.Key, Encoding.UTF8.GetBytes(strValue));
      }
    }

    // Ensure body is not null
    if (string.IsNullOrEmpty(message.Body))
    {
      throw new TechnicalException(
          $"Cannot publish Kafka message: Body is null or empty for {typeof(TIntegrationEvent).Name}");
    }

    // Send to Kafka asynchronously
    try
    {
      var deliveryResult = await _producer.ProduceAsync(
          topic,
          new Message<string, byte[]>
          {
            Headers = confluentheaders,
            Value = Encoding.UTF8.GetBytes(message.Body)
          });

      // Optional: log delivery
      // _logger.LogInformation("Delivered {EventType} to {TopicPartitionOffset}", 
      //     typeof(TIntegrationEvent).Name, deliveryResult.TopicPartitionOffset);
    }
    catch (Exception ex)
    {
      throw new TechnicalException("Failed to publish event to Kafka", ex);
    }
  }
}
