// ✅ General CRUD repository for individual entities
using Franz.Common.Business.Domain;
using Franz.Common.Business.Repositories;
using Franz.Common.Errors;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Repositories;
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
    var entity = await DbContext.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);

    return entity ?? throw new NotFoundException($"Entity {typeof(TEntity).Name} with ID {id} not found.");
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
