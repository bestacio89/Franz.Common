using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Repositories.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Abstractions.Repositories.Implementations
{
  public abstract class ArasAggregateRepository<TAggregate, TEvent>
      : IArasAggregateRepository<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
      where TEvent : BaseDomainEvent
  {
    private readonly IArasAggregateContext _context;

    protected ArasAggregateRepository(IArasAggregateContext context)
    {
      _context = context;
    }

    public virtual Task<TAggregate?> GetAsync(Guid id, CancellationToken ct = default)
        => _context.GetAggregateAsync<TAggregate, TEvent>(id, ct);

    public virtual async Task SaveAsync(TAggregate aggregate, CancellationToken ct = default)
    {
      var lastEvent = aggregate.Events.LastOrDefault();
      if (lastEvent is null)
        return;

      await _context.SaveAggregateAsync<TAggregate, TEvent>(aggregate, ct);
      aggregate.MarkChangesAsCommitted();
    }
  }
}
