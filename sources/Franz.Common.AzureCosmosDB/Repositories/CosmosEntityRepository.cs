using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Repositories;

public class CosmosEntityRepository<TDbContext, TEntity>
    : EntityRepository<TDbContext, TEntity>
    where TDbContext : CosmosDbContextBase
    where TEntity : class, IEntity
{
  public CosmosEntityRepository(TDbContext dbContext)
      : base(dbContext)
  {
  }
}
