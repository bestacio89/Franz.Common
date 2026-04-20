using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

public interface IAggregateRootRepository<TAggregateRoot, TEvent, TId>
    where TAggregateRoot : IAggregateRoot<TEvent>
    where TEvent : class, IDomainEvent
{
  Task<TAggregateRoot> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

  Task SaveAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
}