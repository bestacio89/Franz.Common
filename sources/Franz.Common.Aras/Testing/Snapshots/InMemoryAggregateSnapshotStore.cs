using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using System.Collections.Concurrent;

namespace Franz.Common.Aras.Testing.Snapshots
{
  /// <summary>
  /// In-memory snapshot store for aggregates.
  /// Used for testing snapshot-based rehydration without persistence.
  /// </summary>
  public sealed class InMemoryAggregateSnapshotStore<TAggregate, TDomainEvent>
      : IAggregateSnapshotStore<TAggregate, TDomainEvent>
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    private readonly ConcurrentDictionary<Guid, (TAggregate Aggregate, int Version)> _snapshots = new();

    public Task SaveSnapshotAsync(TAggregate aggregate, CancellationToken ct = default)
    {
      // ⚠️ Note: stores direct reference — consider cloning if immutability matters
      _snapshots[aggregate.Id] = (aggregate, aggregate.Version);
      return Task.CompletedTask;
    }

    public Task<(TAggregate? Aggregate, int Version)> GetLatestSnapshotAsync(
        Guid id, CancellationToken ct = default)
    {
      return Task.FromResult(
          _snapshots.TryGetValue(id, out var snapshot)
              ? (snapshot.Aggregate, snapshot.Version)
              : (null, 0)
      );
    }
  }
}
