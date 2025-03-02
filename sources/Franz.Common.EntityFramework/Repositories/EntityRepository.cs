// ✅ General CRUD repository for individual entities
using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore;

public class EntityRepository<TDbContext, TEntity> : IEntityRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : class, IEntity
{
  protected readonly TDbContext DbContext;

  public EntityRepository(TDbContext dbContext)
  {
    DbContext = dbContext;
  }

  public async Task<TEntity> GetByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    return await DbContext.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);
  }

  public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().Update(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().Remove(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
  }
}
