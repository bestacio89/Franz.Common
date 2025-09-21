using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Abstractions.Repositories.Contracts
{
  public interface IArasEntityRepository<TEntity>
      where TEntity : Entity<Guid>
  {
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
  }
}
