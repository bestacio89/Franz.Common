#nullable enable

using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.EntityFramework.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

public sealed class CosmosSagaDbContext : CosmosDbContextBase
{
  public CosmosSagaDbContext(
      DbContextOptions<CosmosSagaDbContext> options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null)
      : base(options, dispatcher, currentUser)
  {
  }

  public DbSet<CosmosSagaStateDocument> SagaStates => Set<CosmosSagaStateDocument>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<CosmosSagaStateDocument>(entity =>
    {
      entity.ToContainer("sagaStates");

      // Partition key = SagaId
      entity.HasKey(x => x.Id);
      entity.HasPartitionKey(x => x.Id);

      // Cosmos concurrency (etag)
      entity.UseETagConcurrency();

      entity.Property(x => x.SagaType);
      entity.Property(x => x.Payload);
      entity.Property(x => x.ConcurrencyToken);
      entity.Property(x => x.UpdatedAt);
    });
  }
}
