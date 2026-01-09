using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Messaging;

/// <summary>
/// Cosmos EF DbContext for the messaging outbox / inbox mechanism.
/// </summary>
public class CosmosMessagingDbContext : DbContextBase
{
  public CosmosMessagingDbContext(
    DbContextOptions<CosmosMessagingDbContext> options,
    IDispatcher dispatcher,
    ICurrentUserService? currentUser = null)
    : base(options, dispatcher, currentUser)
  {
  }

  public DbSet<StoredMessage> OutboxMessages => Set<StoredMessage>();
  public DbSet<InboxRecord> InboxRecords => Set<InboxRecord>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // OUTBOX
    modelBuilder.Entity<StoredMessage>(entity =>
    {
      entity.ToContainer("OutboxMessages");
      entity.HasKey(m => m.Id);

      // Partition key = id (one message per partition = fine for outbox)
      entity.HasPartitionKey(m => m.Id);

      // Optional: explicit JSON property names
      entity.Property(m => m.Id).ToJsonProperty("id");
      entity.Property(m => m.CreatedOn).ToJsonProperty("createdOn");
      entity.Property(m => m.SentOn).ToJsonProperty("sentOn");
      entity.Property(m => m.RetryCount).ToJsonProperty("retryCount");
      entity.Property(m => m.LastError).ToJsonProperty("lastError");
      entity.Property(m => m.LastTriedOn).ToJsonProperty("lastTriedOn");
      entity.Property(m => m.IsDeadLetter).ToJsonProperty("isDeadLetter");
    });

    // INBOX
    modelBuilder.Entity<InboxRecord>(entity =>
    {
      entity.ToContainer("InboxRecords");
      entity.HasKey(r => r.MessageId);

      entity.HasPartitionKey(r => r.MessageId);
      entity.Property(r => r.MessageId).ToJsonProperty("id");
      entity.Property(r => r.ProcessedOn).ToJsonProperty("processedOn");
    });
  }
}
