using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;

public abstract class AggregateRepository<TDbContext, TAggregateRoot> : IAggregateRepository<TAggregateRoot>
    where TDbContext : DbContext
    where TAggregateRoot : class,IAggregateRoot
{
  protected readonly TDbContext DbContext;

  public AggregateRepository(TDbContext dbContext)
  {
    DbContext = dbContext;
  }

  public async Task<TAggregateRoot> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await DbContext.Set<TAggregateRoot>()
        .IncludeAllRelationships(DbContext) // 
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
  }

  public async Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TAggregateRoot>().AddAsync(aggregateRoot, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
  {
    var existingAggregate = await GetByIdAsync(aggregateRoot.Id, cancellationToken);

    if (existingAggregate == null)
      throw new InvalidOperationException($"Aggregate {typeof(TAggregateRoot).Name} not found.");

    DbContext.Entry(existingAggregate).CurrentValues.SetValues(aggregateRoot);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
  {
    DbContext.Set<TAggregateRoot>().Remove(aggregateRoot);
    await DbContext.SaveChangesAsync(cancellationToken);
  }
}
