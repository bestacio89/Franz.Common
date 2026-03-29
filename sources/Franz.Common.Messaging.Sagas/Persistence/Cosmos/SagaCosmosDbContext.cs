#nullable enable

using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.EntityFramework.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

public sealed class CosmosSagaDbContext : CosmosDbContextBase
{
  public CosmosSagaDbContext(
      DbContextOptions<CosmosSagaDbContext> options,
      IDispatcher dispatcher,
      IOptions<CosmosOptions> cosmosOptions, // 🛠️ Added this to match base
      ICurrentUserService? currentUser = null)
      : base(options, dispatcher, cosmosOptions, currentUser)
  {
  }

  public DbSet<CosmosSagaStateDocument> SagaStates => Set<CosmosSagaStateDocument>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<CosmosSagaStateDocument>(entity =>
    {
      // The base.OnModelCreating already sets a fallback container,
      // but for Sagas we explicitly override it here.
      entity.ToContainer("SagaStates");

      entity.HasKey(x => x.Id);
      entity.HasPartitionKey(x => x.Id);

      entity.UseETagConcurrency();

      entity.Property(x => x.SagaType);
      entity.Property(x => x.Payload);
      entity.Property(x => x.ConcurrencyToken);
      entity.Property(x => x.UpdatedAt);
    });
  }
}