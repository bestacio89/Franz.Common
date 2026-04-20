using Franz.Common.Business.Domain;

namespace Franz.Common.Business.Repositories;

public interface IEntityRepository<TEntity, TId>
    where TEntity : Entity<TId>
{
  Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

  Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

  Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

  Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
