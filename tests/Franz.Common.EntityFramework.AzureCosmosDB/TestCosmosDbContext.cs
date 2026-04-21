using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Franz.Common.AzureCosmosDB.Tests;

public class TestCosmosDbContext : CosmosDbContextBase
{
  public TestCosmosDbContext(
      DbContextOptions<TestCosmosDbContext> options,
      IDispatcher dispatcher,
      IOptions<CosmosOptions> cosmosOptions,
      ICurrentUserService? currentUser = null)
      : base(options, dispatcher, cosmosOptions, currentUser)
  {
  }

  public DbSet<CosmosEntity> Items => Set<CosmosEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // Execute the base logic (Container fallback + conventions)
    base.OnModelCreating(modelBuilder);

  
    // For testing generic repositories, mapping Id as the Partition Key is standard.
    modelBuilder.Entity<CosmosEntity>()
        .ToContainer("Items")
        .HasPartitionKey(x => x.Id);
    modelBuilder.Entity<CosmosEntity>()
    .HasQueryFilter(x => !x.IsDeleted);
  }
}

