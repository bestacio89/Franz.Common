using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

namespace Franz.Common.Aras.Infrastructure.Snapshots
{
  public class ArasInnovatorSnapshotStore<TAggregate, TEvent> : IAggregateSnapshotStore<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>, new()
      where TEvent : IDomainEvent
  {
    public Task SaveSnapshotAsync(TAggregate aggregate, CancellationToken ct = default)
    {
      // TODO: Serialize aggregate state → persist in Innovator
      throw new NotImplementedException();
    }

    public Task<(TAggregate? Aggregate, int Version)> GetLatestSnapshotAsync(Guid id, CancellationToken ct = default)
    {
      // TODO: Fetch persisted snapshot → hydrate aggregate
      throw new NotImplementedException();
    }
  }
}
