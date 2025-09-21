using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
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
  /// </summary>
  public abstract class ArasAggregateContextBase : IArasAggregateContext
  {
    private readonly List<TrackedAggregate> _tracked = new();

    protected ArasAggregateContextBase(IDispatcher dispatcher)
    {
      Dispatcher = dispatcher;
    }

    public IDispatcher Dispatcher { get; }

    public void TrackAggregate<TAggregate, TEvent>(TAggregate aggregate)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
    {
      _tracked.Add(new TrackedAggregate(aggregate, typeof(TAggregate), typeof(TEvent)));
    }

    public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
    {
      foreach (var tracked in _tracked)
      {
        // invoke generic Save via reflection
        var method = GetType()
          .GetMethod(nameof(SaveAggregateAsync))!
          .MakeGenericMethod(tracked.AggregateType, tracked.EventType);

        await (Task)method.Invoke(this, new object[] { tracked.Aggregate, ct })!;
      }

      var count = _tracked.Count;
      _tracked.Clear();
      return count;
    }

    public abstract Task<TAggregate?> GetAggregateAsync<TAggregate, TEvent>(
        Guid id,
        CancellationToken ct = default)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent;

    public abstract Task SaveAggregateAsync<TAggregate, TEvent>(
        TAggregate aggregate,
        CancellationToken ct = default)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent;

    /// <summary>
    /// Returns the snapshot store for the given aggregate type.
    /// Must match the interface constraint exactly.
    /// </summary>
    public abstract IAggregateSnapshotStore<TAggregate, TEvent> SnapshotStore<TAggregate, TEvent>()
        where TAggregate : AggregateRoot<TEvent>, new()
        where TEvent : BaseDomainEvent;

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
