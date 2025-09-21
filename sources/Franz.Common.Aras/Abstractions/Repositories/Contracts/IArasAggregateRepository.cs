using Franz.Common.Business;
using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Abstractions.Repositories.Contracts
{
  public interface IArasAggregateRepository<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
      where TEvent : BaseDomainEvent
  {
    Task<TAggregate?> GetAsync(Guid id, CancellationToken ct = default);

    Task SaveAsync(TAggregate aggregate, CancellationToken ct = default);
  }
}
