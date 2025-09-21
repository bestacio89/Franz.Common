using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Repositories.Contracts;
using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Abstractions.Repositories.Implementations
{
  public abstract class ArasEntityRepository<TEntity> : IArasEntityRepository<TEntity>
      where TEntity : Entity<Guid>
  {
    private readonly IArasEntityContext _context;

    protected ArasEntityRepository(IArasEntityContext context)
    {
      _context = context;
    }

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.GetEntityByIdAsync<TEntity>(id, ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
    {
      // Use a "list all" query convention, e.g. "all items of this type"
      var results = await _context.QueryEntitiesAsync<TEntity>("", ct);
      return results.ToList();
    }

    public virtual Task AddAsync(TEntity entity, CancellationToken ct = default)
        => _context.SaveEntityAsync(entity, ct);

    public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default)
        => _context.SaveEntityAsync(entity, ct);

    public virtual Task DeleteAsync(Guid id, CancellationToken ct = default)
        => _context.DeleteEntityAsync<TEntity>(id, ct);
  }
}
