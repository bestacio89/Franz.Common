using Franz.Common.Mediator.Handlers;


namespace Franz.Common.Business.Events;

public interface IDomainEventHandler<in TEvent> : IEventHandler<TEvent>
  where TEvent : IDomainEvent
{
 



}
