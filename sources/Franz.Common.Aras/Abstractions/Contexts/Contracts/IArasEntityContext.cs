using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Abstractions.Contexts.Contracts
{
  public interface IArasEntityContext : IDisposable
  {
    Task<IReadOnlyCollection<TEntity>> QueryEntitiesAsync<TEntity>(
        string query,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    Task<TEntity?> GetEntityByIdAsync<TEntity>(
        Guid id,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    Task SaveEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    Task DeleteEntityAsync<TEntity>(
        Guid id,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;
  }
}
