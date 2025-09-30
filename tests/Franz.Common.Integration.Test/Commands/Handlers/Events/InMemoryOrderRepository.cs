using Franz.Common.IntegrationTesting.Domain;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;

public sealed class InMemoryOrderRepository
    : IAggregateRootRepository<OrderAggregate, IEvent>
{
  private readonly Dictionary<Guid, List<IEvent>> _store = new();
  private readonly IDispatcher _dispatcher;

  public InMemoryOrderRepository(IDispatcher dispatcher)
  {
    _dispatcher = dispatcher;
  }

  public Task<OrderAggregate> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    _store.TryGetValue(id, out var events);
    var agg = OrderAggregate.Rehydrate(id, events ?? Enumerable.Empty<IEvent>());
    return Task.FromResult(agg);
  }

  public async Task SaveAsync(OrderAggregate aggregate, CancellationToken ct = default)
  {
    if (!_store.TryGetValue(aggregate.Id, out var events))
      events = _store[aggregate.Id] = new List<IEvent>();

    var uncommitted = aggregate.GetUncommittedChanges().ToList();

    // Persist
    events.AddRange(uncommitted);

    // 🚀 Dispatch
    foreach (OrderCancelledEvent ev in uncommitted)
      await _dispatcher.PublishEventAsync(ev, ct);

    aggregate.MarkChangesAsCommitted();
  }
}
