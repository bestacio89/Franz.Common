using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Messages;

public interface IAggregateRootRepository<TAggregateRoot, TEvent>
    where TAggregateRoot : class, IAggregateRoot<TEvent>
    where TEvent : IEvent
{
  Task<TAggregateRoot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default);
}
