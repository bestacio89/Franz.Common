using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Repositories;

namespace Franz.Common.AzureCosmosDB.Repositories;

/// <summary>
/// Cosmos-specific repository built on EF-shaped abstractions,
/// but WITHOUT relational assumptions (no unit-of-work semantics).
/// </summary>
public class CosmosEntityRepository<TDbContext, TEntity, TId>
    : EntityRepository<TDbContext, TEntity, TId>
    where TDbContext : CosmosDbContextBase
    where TEntity : Entity<TId>
{
  public CosmosEntityRepository(TDbContext dbContext)
      : base(dbContext)
  {

  }


  // Optional: future Cosmos-specific overrides can go here
  // (partition hints, optimized queries, etc.)
}