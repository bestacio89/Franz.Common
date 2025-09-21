using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Dispatchers;
using System.Collections.Concurrent;

namespace Franz.Common.Aras.Testing
{
  /// <summary>
  /// In-memory implementation of the ARAS aggregate context.
  /// Designed for testing without a live ARAS instance.
  /// </summary>
  public class InMemoryArasAggregateContext : IArasAggregateContext
  {
    private readonly InMemoryEventStore _eventStore;
    private readonly IDispatcher _dispatcher;
    private readonly List<IAggregateRoot> _tracked = new();

    // Snapshot stores per aggregate/event type pair
    private readonly ConcurrentDictionary<(Type, Type), object> _snapshotStores = new();

    public InMemoryArasAggregateContext(InMemoryEventStore eventStore, IDispatcher dispatcher)
    {
      _eventStore = eventStore;
      _dispatcher = dispatcher;
    }

    public IDispatcher Dispatcher => _dispatcher;

    public void TrackAggregate<TAggregate, TEvent>(TAggregate aggregate)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
    {
      if (!_tracked.Contains(aggregate))
        _tracked.Add(aggregate);
    }

    public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
    {
      int saved = 0;

      foreach (var aggregate in _tracked.OfType<IAggregateRoot>())
      {
        var changes = aggregate.GetUncommittedChanges().ToList();
        if (changes.Any())
        {
          _eventStore.Append(aggregate.Id, changes);

          foreach (var e in changes)
            await _dispatcher.PublishAsync(e, ct);

          aggregate.MarkChangesAsCommitted();
          saved++;
        }
      }

      _tracked.Clear();
      return saved;
    }

    public Task<TAggregate?> GetAggregateAsync<TAggregate, TEvent>(
        Guid id, CancellationToken ct = default)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
    {
      var aggregate = new TAggregate();

      var history = _eventStore.Load(id).OfType<TEvent>().ToList();
      if (history.Any())
      {
        aggregate.ReplayEvents(history);
        return Task.FromResult<TAggregate?>(aggregate);
      }

      return Task.FromResult<TAggregate?>(null);
    }

    public async Task SaveAggregateAsync<TAggregate, TEvent>(
        TAggregate aggregate, CancellationToken ct = default)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
    {
      var lastEvent = aggregate.GetUncommittedChanges().LastOrDefault();
      if (lastEvent is not null)
      {
        _eventStore.Append(aggregate.Id, new[] { lastEvent });
        await _dispatcher.PublishAsync(lastEvent, ct);
        aggregate.MarkChangesAsCommitted();
      }
    }

    public IAggregateSnapshotStore<TAggregate, TEvent> SnapshotStore<TAggregate, TEvent>()
        where TAggregate : AggregateRoot<TEvent>, new()
        where TEvent : BaseDomainEvent
    {
      var key = (typeof(TAggregate), typeof(TEvent));

      var store = (IAggregateSnapshotStore<TAggregate, TEvent>)_snapshotStores.GetOrAdd(
          key,
          _ => new InMemorySnapshotStore<TAggregate, TEvent>()
      );

      return store;
    }

    public void Dispose()
    {
      _tracked.Clear();
      _snapshotStores.Clear();
    }
  }

  /// <summary>
  /// In-memory snapshot store for a specific aggregate/event pair.
  /// Used by InMemoryArasAggregateContext.
  /// </summary>
  
}
