using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

public interface IArasAggregateContext
{
  void TrackAggregate<TAggregate, TDomainEvent>(TAggregate aggregate)
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent;

  Task<TAggregate?> GetAggregateAsync<TAggregate, TDomainEvent>(Guid id, CancellationToken ct = default)
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent;

  Task SaveAggregateAsync<TAggregate, TDomainEvent>(TAggregate aggregate, CancellationToken ct = default)
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent;

  Task<int> SaveAggregateChangesAsync(CancellationToken ct = default);

  IAggregateSnapshotStore<TAggregate, TDomainEvent> SnapshotStore<TAggregate, TDomainEvent>()
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent;
}
