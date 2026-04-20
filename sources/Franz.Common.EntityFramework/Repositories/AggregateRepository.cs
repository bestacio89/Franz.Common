using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Business.Repositories;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.EntityFramework.Repositories;

public abstract class AggregateRepository<TDbContext, TAggregateRoot, TEvent, TId>
    : IAggregateRootRepository<TAggregateRoot, TEvent, TId>
    where TDbContext : DbContextBase
    where TAggregateRoot : AggregateRoot<TEvent>
    where TEvent : class, IDomainEvent
{
  protected readonly TDbContext DbContext;
  private readonly IDispatcher _dispatcher;

  protected AggregateRepository(
      TDbContext dbContext,
      IDispatcher dispatcher)
  {
    DbContext = dbContext;
    _dispatcher = dispatcher;
  }

  // ---------------------------------------------------
  // LOAD (STATE ONLY, NOT EVENT STORE)
  // ---------------------------------------------------
  public virtual async Task<TAggregateRoot> GetByIdAsync(
      TId id,
      CancellationToken cancellationToken = default)
  {
    var entity = await LoadAggregateAsync(id, cancellationToken);

    if (entity is null)
      throw new NotFoundException(
        $"{typeof(TAggregateRoot).Name} with ID {id} not found.");

    return entity;
  }

  // ---------------------------------------------------
  // SAVE (STATE + DISPATCH EVENTS ONLY)
  // ---------------------------------------------------
  public virtual async Task SaveAsync(
      TAggregateRoot aggregateRoot,
      CancellationToken cancellationToken = default)
  {
    await PersistAggregateAsync(aggregateRoot, cancellationToken);

    await DbContext.SaveChangesAsync(cancellationToken);

    var changes = aggregateRoot.GetUncommittedChanges();

    foreach (var ev in changes)
      await _dispatcher.PublishEventAsync(ev, cancellationToken);

    aggregateRoot.MarkChangesAsCommitted();
  }

  // ---------------------------------------------------
  // EXTENSION POINTS (IMPLEMENTED PER AGGREGATE TYPE)
  // ---------------------------------------------------

  protected virtual Task<TAggregateRoot?> LoadAggregateAsync(
      TId id,
      CancellationToken cancellationToken)
  {
    throw new NotImplementedException(
      $"Aggregate loading must be implemented for {typeof(TAggregateRoot).Name}");
  }

  protected virtual Task PersistAggregateAsync(
      TAggregateRoot aggregate,
      CancellationToken cancellationToken)
  {
    // Default assumption: EF tracks the aggregate
    DbContext.Update(aggregate);
    return Task.CompletedTask;
  }
}