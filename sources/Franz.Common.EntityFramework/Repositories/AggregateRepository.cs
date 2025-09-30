using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Microsoft.EntityFrameworkCore;

public abstract class AggregateRepository<TDbContext, TAggregateRoot, TEvent>
    : IAggregateRootRepository<TAggregateRoot, TEvent>
    where TDbContext : DbContext
    where TAggregateRoot : class, IAggregateRoot<TEvent>, new()
    where TEvent : class, IDomainEvent
{
  protected readonly TDbContext DbContext;
  private readonly IDispatcher _dispatcher;

  protected AggregateRepository(TDbContext dbContext, IDispatcher dispatcher)
  {
    DbContext = dbContext;
    _dispatcher = dispatcher;
  }

  public async Task<TAggregateRoot> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    // load historical events from persistence
    var history = await DbContext.Set<TEvent>()
        .Where(e => e.AggregateId == id)
        .OrderBy(e => e.OccurredOn)
        .ToListAsync(cancellationToken);

    if (!history.Any())
      throw new NotFoundException($"{typeof(TAggregateRoot).Name} with ID {id} not found.");

    // rebuild aggregate from events
    var aggregate = new TAggregateRoot();
    aggregate.Rehydrate(id, history);
    return aggregate;
  }

  public async Task SaveAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
  {
    var changes = aggregateRoot.GetUncommittedChanges().ToList();

    if (!changes.Any())
      return;

    // persist new events
    await DbContext.Set<TEvent>().AddRangeAsync(changes, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);

    // dispatch them
    foreach (var ev in changes)
      await _dispatcher.PublishEventAsync(ev, cancellationToken);

    aggregateRoot.MarkChangesAsCommitted();
  }
}
