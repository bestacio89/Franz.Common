#nullable enable

using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Cosmos context for saga state, inbox, and outbox documents. All three entity types
/// deliberately share one container and TenantId partition key so one SaveChanges call
/// can be committed as a native Cosmos transactional batch.
/// </summary>
public sealed class CosmosSagaDbContext : CosmosDbContextBase
{
  public CosmosSagaDbContext(
      DbContextOptions<CosmosSagaDbContext> options,
      IDispatcher dispatcher,
      IOptions<CosmosOptions> cosmosOptions,
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
      entity.ToContainer("SagaStates");
      entity.HasPartitionKey(x => x.TenantId);
      entity.UseETagConcurrency();
      entity.Property(x => x.TenantId).IsRequired();
      entity.Property(x => x.SagaType).IsRequired();
      entity.Property(x => x.StateType).IsRequired();
      entity.Property(x => x.Payload).IsRequired();
      entity.Property(x => x.Status).IsRequired();
      entity.Property(x => x.Version).IsRequired();
      entity.Property(x => x.CreatedAt).IsRequired();
      entity.Property(x => x.UpdatedAt).IsRequired();
    });

    modelBuilder.Entity<CosmosSagaInboxDocument>(entity =>
    {
      entity.ToContainer("SagaStates");
      entity.HasKey(x => x.Id);
      entity.HasPartitionKey(x => x.TenantId);
      entity.Property(x => x.Id).ToJsonProperty("id");
      entity.Property(x => x.TenantId).IsRequired();
      entity.Property(x => x.ProcessedOn).IsRequired();
    });

    modelBuilder.Entity<CosmosSagaOutboxDocument>(entity =>
    {
      entity.ToContainer("SagaStates");
      entity.HasKey(x => x.Id);
      entity.HasPartitionKey(x => x.TenantId);
      entity.Property(x => x.Id).ToJsonProperty("id");
      entity.Property(x => x.TenantId).IsRequired();
      entity.Property(x => x.Payload).IsRequired();
      entity.Property(x => x.CreatedOn).IsRequired();
    });
  }
}
