using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Testing
{
  public class InMemorySnapshotStore<TAggregate, TEvent> :
      IAggregateSnapshotStore<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
      where TEvent : BaseDomainEvent
  {
    private readonly ConcurrentDictionary<Guid, (TAggregate Aggregate, int Version)> _snapshots = new();

    public Task<(TAggregate? Aggregate, int Version)> GetLatestSnapshotAsync(
        Guid id,
        CancellationToken ct = default)
    {
      if (_snapshots.TryGetValue(id, out var snapshot))
        return Task.FromResult<(TAggregate?, int)>((snapshot.Aggregate, snapshot.Version));

      return Task.FromResult<(TAggregate?, int)>((null, 0));
    }

    public Task SaveSnapshotAsync(TAggregate aggregate, CancellationToken ct = default)
    {
      var version = aggregate.Version;
      _snapshots[aggregate.Id] = (aggregate, version);
      return Task.CompletedTask;
    }
  }
}
