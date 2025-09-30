using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Dispatchers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Abstractions.Contexts.Implementations
{
  /// <summary>
  /// Base class for ARAS aggregate contexts, handling tracking,
  /// saving, and snapshot-aware loading of aggregates.
  /// Aggregates always raise domain events; entities do not.
  /// </summary>
  public abstract class ArasAggregateContextBase : IArasAggregateContext
  {
    private readonly List<TrackedAggregate> _tracked = new();

    protected ArasAggregateContextBase(IDispatcher dispatcher)
    {
      Dispatcher = dispatcher;
    }

    public IDispatcher Dispatcher { get; }

    /// <summary>
    /// Track an aggregate instance so that its changes can be saved later.
    /// </summary>
    public void TrackAggregate<TAggregate, TDomainEvent>(TAggregate aggregate)
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      _tracked.Add(new TrackedAggregate(aggregate, typeof(TAggregate), typeof(TDomainEvent)));
    }

    /// <summary>
    /// Saves all tracked aggregates by invoking SaveAggregateAsync
    /// on each concrete aggregate type.
    /// </summary>
    public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
    {
      foreach (var tracked in _tracked)
      {
        var method = GetType()
          .GetMethod(nameof(SaveAggregateAsync))!
          .MakeGenericMethod(tracked.AggregateType, tracked.EventType);

        await (Task)method.Invoke(this, new object[] { tracked.Aggregate, ct })!;
      }

      var count = _tracked.Count;
      _tracked.Clear();
      return count;
    }

    /// <summary>
    /// Load an aggregate by its identifier, replaying its event history or snapshot.
    /// </summary>
    public abstract Task<TAggregate?> GetAggregateAsync<TAggregate, TDomainEvent>(
        Guid id,
        CancellationToken ct = default)
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent;

    /// <summary>
    /// Save a single aggregate’s uncommitted changes and dispatch its events.
    /// </summary>
    public abstract Task SaveAggregateAsync<TAggregate, TDomainEvent>(
        TAggregate aggregate,
        CancellationToken ct = default)
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent;

    /// <summary>
    /// Returns the snapshot store for the given aggregate type.
    /// </summary>
    public abstract IAggregateSnapshotStore<TAggregate, TDomainEvent> SnapshotStore<TAggregate, TDomainEvent>()
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent;

    public void Dispose()
    {
      _tracked.Clear();
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }

    private record TrackedAggregate(object Aggregate, Type AggregateType, Type EventType);
  }
}
