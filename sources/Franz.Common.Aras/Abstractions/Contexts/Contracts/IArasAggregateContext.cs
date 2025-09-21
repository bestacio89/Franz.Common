using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Dispatchers;

namespace Franz.Common.Aras.Abstractions.Contexts.Contracts
{
  /// <summary>
  /// Contract for managing ARAS aggregates with DDD and event sourcing semantics.
  /// Provides a unit-of-work style API similar to EF's DbContext,
  /// but specialized for aggregates and domain events.
  /// </summary>
  public interface IArasAggregateContext : IDisposable
  {
    /// <summary>
    /// Dispatcher for propagating domain events into Franz pipelines
    /// (logging, transactions, messaging, etc.).
    /// </summary>
    IDispatcher Dispatcher { get; }

    /// <summary>
    /// Tracks an aggregate for persistence (unit of work style).
    /// </summary>
    void TrackAggregate<TAggregate, TEvent>(TAggregate aggregate)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent;

    /// <summary>
    /// Persists all tracked aggregates and dispatches their uncommitted events.
    /// Returns the number of aggregates saved (EF-style).
    /// </summary>
    Task<int> SaveAggregateChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves an aggregate by ARAS identifier, hydrating it from state
    /// and/or replaying domain events.
    /// </summary>
    Task<TAggregate?> GetAggregateAsync<TAggregate, TEvent>(
        Guid id,
        CancellationToken ct = default
    )
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent;

    /// <summary>
    /// Persists a single aggregate and dispatches its most recent domain event.
    /// </summary>
    Task SaveAggregateAsync<TAggregate, TEvent>(
        TAggregate aggregate,
        CancellationToken ct = default
    )
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent;
    IAggregateSnapshotStore<TAggregate, TEvent> SnapshotStore<TAggregate, TEvent>()
        where TAggregate : AggregateRoot<TEvent>, new()
        where TEvent : BaseDomainEvent;
  }
}
