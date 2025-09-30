using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Repositories.Contracts;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

namespace Franz.Common.Aras.Abstractions.Repositories.Implementations
{
  /// <summary>
  /// Base ARAS repository for aggregates.
  /// Delegates persistence and event dispatch to the underlying context.
  /// </summary>
  public abstract class ArasAggregateRepository<TAggregate, TDomainEvent>
      : IArasAggregateRepository<TAggregate, TDomainEvent>
      where TAggregate : AggregateRoot<TDomainEvent>, IAggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    private readonly IArasAggregateContext _context;

    protected ArasAggregateRepository(IArasAggregateContext context)
    {
      _context = context;
    }

    /// <summary>
    /// Load aggregate by ID via context.
    /// </summary>
    public virtual Task<TAggregate?> GetAsync(Guid id, CancellationToken ct = default)
        => _context.GetAggregateAsync<TAggregate, TDomainEvent>(id, ct);

    /// <summary>
    /// Save aggregate via context.
    /// Will persist state and dispatch *all* uncommitted domain events.
    /// </summary>
    public virtual async Task SaveAsync(TAggregate aggregate, CancellationToken ct = default)
    {
      if (!aggregate.GetUncommittedChanges().Any())
        return;

      await _context.SaveAggregateAsync<TAggregate, TDomainEvent>(aggregate, ct);
      // no need to call MarkChangesAsCommitted here:
      // the context already takes care of it
    }
  }
}
