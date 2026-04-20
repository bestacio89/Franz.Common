using Franz.Common.Business.Domain;
using Franz.Common.Business.Repositories;
using Franz.Common.Errors;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Repositories;

public class EntityRepository<TDbContext, TEntity, TId>
    : IEntityRepository<TEntity, TId>
    where TDbContext : DbContextBase
    where TEntity : Entity<TId>
{
  protected readonly TDbContext DbContext;

  public EntityRepository(TDbContext dbContext)
  {
    DbContext = dbContext;
  }



  public async Task<TEntity> GetByIdAsync(
      TId id,
      CancellationToken cancellationToken = default)
  {
    var entity = await DbContext.Set<TEntity>()
        .FindAsync(new object[] { id }, cancellationToken);

    if (entity is null)
      throw new NotFoundException(
          $"{typeof(TEntity).Name} with id '{id}' was not found.");

    return entity;
  }
  public async Task<IReadOnlyList<TEntity>> GetAll(CancellationToken cancellationToken)
  {
    return await DbContext
        .Set<TEntity>()
        .AsNoTracking()
        .ToListAsync(cancellationToken);
  }
  public async Task AddAsync(
      TEntity entity,
      CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TEntity>()
        .AddAsync(entity, cancellationToken);

    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateAsync(
      TEntity entity,
      CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().Update(entity);

    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(
      TEntity entity,
      CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().Remove(entity);

    await DbContext.SaveChangesAsync(cancellationToken);
  }
}