#nullable enable
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

  // 🛠️ FIX: Parameter changed from string to Guid to match IInboxStore
  public async Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    return await _dbContext.InboxRecords
        .AsNoTracking()
        .AnyAsync(r => r.MessageId == messageId, ct);
  }

  // 🛠️ FIX: Parameter changed from string to Guid to match IInboxStore
  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    // 🚀 BAZOOKA REFACTOR: Direct lookup using Guid v7
    var exists = await _dbContext.InboxRecords
        .AnyAsync(r => r.MessageId == messageId, ct);

    if (!exists)
    {
      await _dbContext.InboxRecords.AddAsync(new InboxRecord
      {
        // Assigning native Guid
        MessageId = messageId,
        ProcessedOn = DateTime.UtcNow
      }, ct);

      await _dbContext.SaveChangesAsync(ct);
    }
  }
}