using Franz.Common.Business.Domain;
using Franz.Common.Business.Repositories;
using Franz.Common.Errors;
using Franz.Common.EntityFramework.Auditing;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework.Repositories;

public class EntityRepository<TDbContext, TEntity, TId>
    : IEntityRepository<TEntity, TId>,
      IEntityFrameworkBulkUpdateRepository<TEntity>,
      IEntityFrameworkBulkDeleteRepository<TEntity>
    where TDbContext : DbContextBase
    where TEntity : Entity<TId>
{
  protected readonly TDbContext DbContext;
  private readonly ICurrentUserService? _currentUserService;

  public EntityRepository(TDbContext dbContext)
      : this(dbContext, null)
  {
  }

  public EntityRepository(TDbContext dbContext, ICurrentUserService? currentUserService)
  {
    DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _currentUserService = currentUserService;
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

    if (!update.HasAssignments)
      throw new InvalidOperationException("A bulk update must define at least one property assignment.");

    var modifiedAt = DateTimeOffset.UtcNow;
    var modifiedBy = _currentUserService?.UserId ?? "system";
    update.SetFrameworkProperty(entity => entity.LastModifiedDate, modifiedAt);
    update.SetFrameworkProperty(entity => entity.LastModifiedBy, modifiedBy);
    update.AddConcurrencyVersionIncrement();

    return DbContext.Set<TEntity>()
        .Where(predicate)
        .ExecuteUpdateAsync(update.ApplyTo, cancellationToken);
  }

  /// <inheritdoc />
  public Task<int> ExecuteBulkDeleteAsync(
      Expression<Func<TEntity, bool>> predicate,
      bool hardDelete = false,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    var query = DbContext.Set<TEntity>().Where(predicate);
    if (hardDelete)
      return query.ExecuteDeleteAsync(cancellationToken);

    var deletedAt = DateTimeOffset.UtcNow;
    var deletedBy = _currentUserService?.UserId ?? "system";
    var update = new BulkUpdateBuilder<TEntity>()
        .SetFrameworkProperty(entity => entity.IsDeleted, true)
        .SetFrameworkProperty(entity => entity.DateDeleted, deletedAt)
        .SetFrameworkProperty(entity => entity.DeletedBy, deletedBy)
        .SetFrameworkProperty(entity => entity.LastModifiedDate, deletedAt)
        .SetFrameworkProperty(entity => entity.LastModifiedBy, deletedBy);

    update.AddConcurrencyVersionIncrement();

    return query.ExecuteUpdateAsync(update.ApplyTo, cancellationToken);
  }

  public async Task DeleteAsync(TEntity entity, bool hardDelete = false, CancellationToken cancellationToken = default)
  {
    if (!hardDelete)
    {
      entity.MarkDeleted(_currentUserService?.UserId ?? "system");
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
