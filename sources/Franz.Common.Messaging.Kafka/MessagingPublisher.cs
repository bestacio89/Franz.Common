using Franz.Common.Business.Events;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using System;
using Franz.Common.Messaging.Kafka.Modeling;
using System.Text;
using Confluent.Kafka;
using System.Collections.Generic;

namespace Franz.Common.Messaging.Kafka
{
  public class MessagingPublisher : IMessagingPublisher
  {
    private readonly IProducer<string, byte[]> _producer;
    private readonly IMessagingInitializer _messagingInitializer;
    private readonly IMessageFactory _messageFactory;
    private readonly IMessageHandler _messageHandler;

    
      public MessagingPublisher(
        IProducer<string, byte[]> producer,
        IMessagingInitializer messagingInitializer,
        IMessageFactory messageFactory,
        IMessageHandler messageHandler)
    {
      _producer = producer;
      _messagingInitializer = messagingInitializer;
      _messageFactory = messageFactory;
      _messageHandler = messageHandler;
    }

    public void Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
    where TIntegrationEvent : BaseEvent, IIntegrationEvent
    {
      _messagingInitializer.Initialize();

      var message = _messageFactory.Build(integrationEvent);

      _messageHandler.Process(message);

      var integrationEventAssembly = integrationEvent.GetType().Assembly;
      var topic = TopicNamer.GetTopicName(integrationEventAssembly);

      
      var headers = message.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());

      var confluentHeaders = new Confluent.Kafka.Headers();
      foreach (var header in headers)
      {
        confluentHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString()));
      }
      var deliveryReport = _producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = confluentHeaders, Value = Encoding.UTF8.GetBytes(message.Body) });
      deliveryReport.ContinueWith(task =>
      {
        if (task.IsCompletedSuccessfully)
        {
          _producer.Flush();
        }
        else
        {
          throw new Exception("Failed to Publish event", task.Exception);
        }
      });
    }
  }
}
