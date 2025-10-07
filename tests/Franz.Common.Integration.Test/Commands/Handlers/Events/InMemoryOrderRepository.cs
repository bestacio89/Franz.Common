using France.Common.Extensions;
using Franz.Common.Business.Events;
using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.Mediator.Dispatchers;
using System.Collections.Generic;

public sealed class InMemoryOrderRepository
    : IAggregateRootRepository<OrderAggregate, IDomainEvent>
{
  private readonly Dictionary<Guid, List<IDomainEvent>> _store = new();
  private readonly IDispatcher _dispatcher;

  public InMemoryOrderRepository(IDispatcher dispatcher)
  {
    _dispatcher = dispatcher;
  }

  public Task<OrderAggregate> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    _store.TryGetValue(id, out var events);
    var agg = OrderAggregate.Rehydrate(id, events ?? Enumerable.Empty<IDomainEvent>());
    return Task.FromResult(agg);
  }

  public async Task SaveAsync(OrderAggregate aggregate, CancellationToken ct = default)
  {
    if (!_store.TryGetValue(aggregate.Id, out var events))
      events = _store[aggregate.Id] = new List<IDomainEvent>();

    var uncommitted = aggregate.GetUncommittedChanges().ToList();

    // Persist
    events.AddRange((IEnumerable<IDomainEvent>)uncommitted);

    // 🚀 Dispatch
    foreach (var ev in uncommitted)
      await _dispatcher.PublishEventAsync(ev, ct);

    aggregate.MarkChangesAsCommitted();
  }
}
