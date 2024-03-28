using Franz.Common.Business.Events;

namespace Franz.Common.Messaging;

public interface IMessagingPublisher
{
  void Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
    where TIntegrationEvent : BaseEvent, IIntegrationEvent;
}
