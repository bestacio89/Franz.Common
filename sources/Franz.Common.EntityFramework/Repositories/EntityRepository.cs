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

  // =========================
  // READ
  // =========================

  public async Task<TEntity> GetByIdAsync(
      TId id,
      CancellationToken cancellationToken = default)
  {
    var entity = await DbContext.Set<TEntity>()
        .FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);

    if (entity is null)
      throw new NotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");

    return entity;
  }

  public async Task<IReadOnlyList<TEntity>> GetAllAsync(
      CancellationToken cancellationToken = default)
  {
    return await DbContext.Set<TEntity>()
        .Where(x => !x.IsDeleted)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
  }

  // =========================
  // CREATE
  // =========================

  public async Task AddAsync(
      TEntity entity,
      CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task AddRangeAsync(
      IEnumerable<TEntity> entities,
      CancellationToken cancellationToken = default)
  {
    await DbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  // =========================
  // UPDATE
  // =========================

  public async Task UpdateAsync(
      TEntity entity,
      CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().Update(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateRangeAsync(
      IEnumerable<TEntity> entities,
      CancellationToken cancellationToken = default)
  {
    DbContext.Set<TEntity>().UpdateRange(entities);
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  // =========================
  // DELETE (SOFT BY DEFAULT)
  // =========================

  public async Task DeleteAsync(
      TEntity entity,
      bool hardDelete = false,
      CancellationToken cancellationToken = default)
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

  public async Task DeleteRangeAsync(
      IEnumerable<TEntity> entities,
      bool hardDelete = false,
      CancellationToken cancellationToken = default)
  {
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