using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

namespace Franz.Common.Aras.Abstractions.Repositories.Contracts
{
  public interface IArasAggregateRepository<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot<TEvent>, new()
      where TEvent : IDomainEvent
  {
    Task<TAggregate?> GetAsync(Guid id, CancellationToken ct = default);

    Task SaveAsync(TAggregate aggregate, CancellationToken ct = default);
  }
}
