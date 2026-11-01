using Franz.Common.Business.Domain;
using Franz.Common.Business.Repositories;
using Franz.Common.Errors;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework.Repositories;

public class EntityRepository<TDbContext, TEntity, TId>
    : IEntityRepository<TEntity, TId>,
      IEntityFrameworkBulkUpdateRepository<TEntity>
    where TDbContext : DbContextBase
    where TEntity : Entity<TId>
{
  protected readonly TDbContext DbContext;

  public EntityRepository(TDbContext dbContext)
  {
    DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<TEntity> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
  {
    var entity = await DbContext.Set<TEntity>()
        .FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);

    if (entity is null)
      throw new NotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");

    return entity;
  }

  public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    return await DbContext.Set<TEntity>()
        .Where(x => !x.IsDeleted)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
  }

  public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().Update(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().UpdateRange(entities);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  /// <inheritdoc />
  public Task<int> ExecuteBulkUpdateAsync(
      Expression<Func<TEntity, bool>> predicate,
      Action<BulkUpdateBuilder<TEntity>> configure,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentNullException.ThrowIfNull(configure);

    var update = new BulkUpdateBuilder<TEntity>();
    configure(update);

    return DbContext.Set<TEntity>()
        .Where(predicate)
        .ExecuteUpdateAsync(update.ApplyTo, cancellationToken);
  }

  public async Task DeleteAsync(TEntity entity, bool hardDelete = false, CancellationToken cancellationToken = default)
  {
    if (!hardDelete)
    {
      entity.MarkDeleted("system");
      DbContext.Set<TEntity>().Update(entity);
    }
    else
    {
      DbContext.Set<TEntity>().Remove(entity);
    }

    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, bool hardDelete = false, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entities);

    if (!hardDelete)
    {
      foreach (var entity in entities)
        entity.MarkDeleted("system");

      DbContext.Set<TEntity>().UpdateRange(entities);
    }
    else
    {
      DbContext.Set<TEntity>().RemoveRange(entities);
    }

    await DbContext.SaveChangesAsync(cancellationToken);
  }
}
