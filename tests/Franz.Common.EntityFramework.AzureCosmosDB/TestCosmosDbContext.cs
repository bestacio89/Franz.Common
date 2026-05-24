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
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<CosmosEntity>()
        .ToContainer("Items")
        .HasPartitionKey(x => x.Id);

    modelBuilder.Entity<CosmosEntity>()
        .HasQueryFilter(x => !x.IsDeleted);
  }

  // 🔥 IMPORTANT ADDITION (COSMOS TEST STABILITY)
  public override async ValueTask DisposeAsync()
  {
    // Ensure pending changes are flushed before teardown
    if (ChangeTracker.HasChanges())
    {
      try
      {
        await SaveChangesAsync();
      }
      catch
      {
        // swallow intentionally: test teardown safety > strict consistency
      }
    }

    await base.DisposeAsync();
  }
}