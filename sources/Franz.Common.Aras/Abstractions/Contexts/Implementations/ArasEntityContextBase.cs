using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Business.Domain;
namespace Franz.Common.Aras.Abstractions.Contexts.Implementations
{
  public abstract class ArasEntityContextBase : IArasEntityContext
  {
    public abstract Task<IReadOnlyCollection<TEntity>> QueryEntitiesAsync<TEntity>(
        string query,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    public abstract Task<TEntity?> GetEntityByIdAsync<TEntity>(
        Guid id,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    public abstract Task SaveEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    public abstract Task DeleteEntityAsync<TEntity>(
        Guid id,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>;

    public abstract void Dispose();
  }
}
