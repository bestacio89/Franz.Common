using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;
using System.Diagnostics.CodeAnalysis;


public interface IAggregateRootRepository<TAggregateRoot, TEvent>
    where TAggregateRoot : class, IAggregateRoot<TEvent>, new()
    where TEvent : class, IDomainEvent
{
  Task<TAggregateRoot> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task SaveAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default);
}
