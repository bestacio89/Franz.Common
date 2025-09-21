using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using System.Collections.Concurrent;
namespace Franz.Common.Aras.Testing.Snapshots;
public class InMemoryAggregateSnapshotStore<TAggregate, TEvent> : IAggregateSnapshotStore<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>, new()
      where TEvent : BaseDomainEvent
  {
    private readonly ConcurrentDictionary<Guid, (TAggregate Aggregate, int Version)> _snapshots = new();

    public Task SaveSnapshotAsync(TAggregate aggregate, CancellationToken ct = default)
    {
      _snapshots[aggregate.Id] = (aggregate, aggregate.Version);
      return Task.CompletedTask;
    }

    public Task<(TAggregate? Aggregate, int Version)> GetLatestSnapshotAsync(Guid id, CancellationToken ct = default)
    {
      return Task.FromResult(
          _snapshots.TryGetValue(id, out var snapshot)
              ? (snapshot.Aggregate, snapshot.Version)
              : (null, 0)
      );
    }
  }