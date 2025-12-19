using Confluent.Kafka;
using Franz.Common.Business.Domain;
using Franz.Common.Errors; // TechnicalException
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Factories;
using System.Text;

namespace Franz.Common.Messaging.Kafka;

public class MessagingPublisher(
    IProducer<string, byte[]> producer,
    IMessagingInitializer messagingInitializer,
    IMessageFactory messageFactory,
    IDispatcher dispatcher)
  : IMessagingPublisher
{
  private readonly IProducer<string, byte[]> _producer = producer;
  private readonly IMessagingInitializer _messagingInitializer = messagingInitializer;
  private readonly IMessageFactory _messageFactory = messageFactory;
  private readonly IDispatcher _dispatcher = dispatcher;

  public async Task Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
      where TIntegrationEvent : IIntegrationEvent
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
    var confluentHeaders = new Confluent.Kafka.Headers();
    foreach (var header in message.Headers)
    {
      var strValue = header.Value.ToString();
      if (!string.IsNullOrEmpty(strValue))
      {
        confluentHeaders.Add(header.Key, Encoding.UTF8.GetBytes(strValue));
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
            Headers = confluentHeaders,
            Value = Encoding.UTF8.GetBytes(message.Body)
          });

      // Optional logging hook here
      // _logger.LogInformation("Delivered {EventType} to {TopicPartitionOffset}", 
      //   typeof(TIntegrationEvent).Name, deliveryResult.TopicPartitionOffset);
    }
    catch (Exception ex)
    {
      throw new TechnicalException("Failed to publish event to Kafka", ex);
    }
  }
}
