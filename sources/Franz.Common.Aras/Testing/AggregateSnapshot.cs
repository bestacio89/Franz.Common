using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using System;

namespace Franz.Common.Aras.Testing
{
  public class AggregateSnapshot<TAggregate, TEvent> : IAggregateSnapshot
      where TAggregate : AggregateRoot<TEvent>, new()
      where TEvent : BaseDomainEvent
  {
    public Guid AggregateId { get; }
    public int Version { get; }
    public DateTime Timestamp { get; }
    public TAggregate Aggregate { get; }

    public AggregateSnapshot(TAggregate aggregate)
    {
      Aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
      AggregateId = aggregate.Id;
      Version = aggregate.Version;
      Timestamp = DateTime.UtcNow; // snapshot creation time
    }
  }
}
