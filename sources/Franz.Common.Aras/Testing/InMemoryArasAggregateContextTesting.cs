using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Aras.Testing.Snapshots;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Dispatchers;
using System.Collections.Concurrent;

namespace Franz.Common.Aras.Testing
{
  /// <summary>
  /// In-memory implementation of the ARAS aggregate context.
  /// Designed for testing without a live ARAS instance.
  /// Persists events in memory and dispatches them through Franz pipelines.
  /// </summary>
  public sealed class InMemoryArasAggregateContext : IArasAggregateContext, IDisposable
  {
    private readonly InMemoryEventStore _eventStore;
    private readonly IDispatcher _dispatcher;
    private readonly List<object> _tracked = new();

    // Snapshot stores keyed by (Aggregate, Event) type
    private readonly ConcurrentDictionary<(Type, Type), object> _snapshotStores = new();

    public InMemoryArasAggregateContext(InMemoryEventStore eventStore, IDispatcher dispatcher)
    {
      _eventStore = eventStore;
      _dispatcher = dispatcher;
    }

    public IDispatcher Dispatcher => _dispatcher;

    public void TrackAggregate<TAggregate, TDomainEvent>(TAggregate aggregate)
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      if (!_tracked.Contains(aggregate))
        _tracked.Add(aggregate);
    }

    public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
    {
      int saved = 0;

      foreach (var aggregateObj in _tracked)
      {
        switch (aggregateObj)
        {
          case AggregateRoot<IDomainEvent> agg:
            var changes = agg.GetUncommittedChanges().ToList();
            if (changes.Any())
            {
              _eventStore.Append(agg.Id, changes);

              foreach (var e in changes)
                await _dispatcher.PublishEventAsync(e, ct);

              agg.MarkChangesAsCommitted();
              saved++;
            }
            break;
        }
      }

      _tracked.Clear();
      return saved;
    }

    public Task<TAggregate?> GetAggregateAsync<TAggregate, TDomainEvent>(
        Guid id, CancellationToken ct = default)
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      var aggregate = new TAggregate();

      var history = _eventStore.Load(id).OfType<TDomainEvent>().ToList();
      if (history.Any())
      {
        aggregate.ReplayEvents(history);
        return Task.FromResult<TAggregate?>(aggregate);
      }

      return Task.FromResult<TAggregate?>(null);
    }

    public async Task SaveAggregateAsync<TAggregate, TDomainEvent>(
    TAggregate aggregate, CancellationToken ct = default)
    where TAggregate : AggregateRoot<TDomainEvent>, new()
    where TDomainEvent : IDomainEvent
    {
      var changes = aggregate.GetUncommittedChanges().ToList();
      if (changes.Any())
      {
        // ✅ Cast to IEnumerable<IDomainEvent> since List<TDomainEvent> is not covariant
        _eventStore.Append(aggregate.Id, changes.Cast<IDomainEvent>());

        foreach (var ev in changes)
        {
          // Publish with the correct concrete type via dispatcher
          await _dispatcher.PublishEventAsync(ev, ct);
        }

        aggregate.MarkChangesAsCommitted();
      }
    }


    public IAggregateSnapshotStore<TAggregate, TDomainEvent> SnapshotStore<TAggregate, TDomainEvent>()
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      var key = (typeof(TAggregate), typeof(TDomainEvent));

      var store = (IAggregateSnapshotStore<TAggregate, TDomainEvent>)_snapshotStores.GetOrAdd(
          key,
          _ => new InMemoryAggregateSnapshotStore<TAggregate, TDomainEvent>()
      );

      return store;
    }

    public void Dispose()
    {
      _tracked.Clear();
      _snapshotStores.Clear();
    }
  }
}
