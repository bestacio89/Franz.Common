using Franz.Common.Business.Domain;

namespace Franz.Common.Business.Repositories;

public interface IEntityRepository<TEntity, TId>
    where TEntity : Entity<TId>
{
  Task<TEntity> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

  Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
  Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

  Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
  Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

  Task DeleteAsync(TEntity entity, bool hardDelete = false, CancellationToken cancellationToken = default);
  Task DeleteRangeAsync(IEnumerable<TEntity> entities, bool hardDelete = false, CancellationToken cancellationToken = default);
}