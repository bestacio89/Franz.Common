using Franz.Common.DependencyInjection;

namespace Franz.Common.Business.Domain;

public interface IAggregateRepository<TAggregateRoot> : IScopedDependency
  where TAggregateRoot : IAggregateRoot
{
  
  Task Update<TEntity>(TEntity entity, CancellationToken cancellationToken);
  Task SaveChangesAsync(CancellationToken cancellationToken);

  Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    where TEntity : IEntity;

  Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken)

    where TEntity : IEntity;
  void Remove<TEntity>(TEntity entity) where TEntity : IEntity;

  void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : IEntity;
}
