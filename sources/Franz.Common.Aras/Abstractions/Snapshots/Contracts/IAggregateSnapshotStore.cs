using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Abstractions.Snapshots.Contracts;
public interface IAggregateSnapshotStore<TAggregate, TEvent>
       where TAggregate : AggregateRoot<TEvent>, new()
       where TEvent : IDomainEvent
{
  Task SaveSnapshotAsync(TAggregate aggregate, CancellationToken ct = default);
  Task<(TAggregate? Aggregate, int Version)> GetLatestSnapshotAsync(Guid id, CancellationToken ct = default);
}