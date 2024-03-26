using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Franz.Common.EntityFramework.Repositories;
public abstract class AggregateRepository<TDbContext, TAggregateRoot> : IAggregateRepository<TAggregateRoot>
  where TDbContext : DbContext
  where TAggregateRoot : IAggregateRoot
{
  protected TDbContext DbContext { get; }

  public AggregateRepository(TDbContext dbContext)
  {
    DbContext = dbContext;
  }

  public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
    where TEntity : IEntity
  {
    await DbContext.AddAsync(entity, cancellationToken);
  }

  public async Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    where TEntity : IEntity
  {
    await DbContext.AddRangeAsync(entities, cancellationToken);
  }

  public void Remove<TEntity>(TEntity entity)
    where TEntity : IEntity
  {
    DbContext.Remove(entity);
  }

  public void RemoveRange<TEntity>(IEnumerable<TEntity> entities)
    where TEntity : IEntity
  {
    DbContext.RemoveRange(entities);
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<TEntity> Update<TEntity>(TEntity entity, CancellationToken cancellation)
  {
#pragma warning disable CS8604 // Possible null reference argument.
    DbContext.Entry(entity).State = EntityState.Modified;
#pragma warning restore CS8604 // Possible null reference argument.
    await DbContext.SaveChangesAsync();
    return entity;

  }


  Task IAggregateRepository<TAggregateRoot>.Update<TEntity>(TEntity entity, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
