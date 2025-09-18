using Franz.Common.Business.Events;
using Franz.Common.Mediator;

namespace Franz.Common.Messaging;

public interface IMessagingPublisher
{
  /// <summary>
  /// Publishes an integration event through the Franz mediator pipeline 
  /// and then forwards it to the external messaging system (e.g., Kafka).
  /// </summary>
  Task Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
      where TIntegrationEvent : BaseDomainEvent, IIntegrationEvent;
}
