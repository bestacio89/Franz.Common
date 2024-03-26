using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Repositories;

public class ReadRepository<TEntity>(DbContext dbContext) : IReadRepository<TEntity>
  where TEntity : class, IEntity
{
  public readonly DbContext dbContext = dbContext;

  public virtual async Task<IQueryable<TEntity>> GetAll(CancellationToken cancellation)
  {
    var result = await dbContext
      .Set<TEntity>()
      .AsNoTracking()
      .ToListAsync(cancellationToken: cancellation);

    return (IQueryable<TEntity>) result;
  }

  public async Task<TEntity> GetEntity(int id)
  {
    var result = await dbContext
      .Set<TEntity>()
      .FindAsync(id);
#pragma warning disable CS8603 // Possible null reference return.
    return result;
#pragma warning restore CS8603 // Possible null reference return.
    
  }
}
