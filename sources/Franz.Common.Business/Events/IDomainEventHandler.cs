using Franz.Common.Mediator.Messages;

namespace Franz.Common.Business.Events;

public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
  where TEvent : IDomainEvent
{
 



}
