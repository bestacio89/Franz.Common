using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;

public interface IAggregateRepository<TAggregate, TEvent>
    where TAggregate : AggregateRoot<TEvent>
    where TEvent : IDomainEvent, IEvent
{
  Task<TAggregate?> GetByIdAsync(Guid id);
  Task SaveAsync(TAggregate aggregate);
  Task DeleteAsync(Guid id);
}
