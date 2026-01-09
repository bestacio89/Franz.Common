using Franz.Common.Messaging;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Messaging;

public class CosmosEfInboxStore : IInboxStore
{
  private readonly CosmosMessagingDbContext _dbContext;

  public CosmosEfInboxStore(CosmosMessagingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<bool> HasProcessedAsync(string messageId, CancellationToken ct = default)
  {
    return await _dbContext.InboxRecords
      .AsNoTracking()
      .AnyAsync(r => r.MessageId == messageId, ct);
  }

  public async Task MarkProcessedAsync(string messageId, CancellationToken ct = default)
  {
    // Idempotent insert (Cosmos will throw on duplicate key; we can ignore if needed)
    var exists = await _dbContext.InboxRecords
      .AnyAsync(r => r.MessageId == messageId, ct);

    if (!exists)
    {
      await _dbContext.InboxRecords.AddAsync(new InboxRecord
      {
        MessageId = messageId,
        ProcessedOn = DateTime.UtcNow
      }, ct);

      await _dbContext.SaveChangesAsync(ct);
    }
  }
}
