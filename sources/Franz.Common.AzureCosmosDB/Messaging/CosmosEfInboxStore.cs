#nullable enable

using System.Net;
using Franz.Common.Messaging.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Messaging;

/// <summary>
/// Cosmos EF-backed inbox. Message identity is also the logical partition key,
/// allowing a single atomic create operation to arbitrate concurrent deliveries.
/// </summary>
public sealed class CosmosEfInboxStore : IInboxStore
{
  private readonly CosmosMessagingDbContext _dbContext;

  public CosmosEfInboxStore(CosmosMessagingDbContext dbContext)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);
    return _dbContext.InboxRecords
        .AsNoTracking()
        .AnyAsync(record => record.MessageId == messageId, ct);
  }

  public async Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);

    var record = new InboxRecord
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    };

    await _dbContext.InboxRecords.AddAsync(record, ct).ConfigureAwait(false);

    try
    {
      await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
      return true;
    }
    catch (DbUpdateException exception) when (IsConflict(exception))
    {
      // A failed EF insert remains tracked as Added. Detach it so a later
      // SaveChanges call on this scoped context does not replay the conflict.
      _dbContext.Entry(record).State = EntityState.Detached;
      return false;
    }
  }

  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);

    // Preserve the historical idempotent behavior of this provider's method.
    _ = await TryRegisterAsync(messageId, ct).ConfigureAwait(false);
  }

  private static bool IsConflict(DbUpdateException exception)
      => exception.GetBaseException() is CosmosException
      {
        StatusCode: HttpStatusCode.Conflict
      };

  private static void ValidateMessageId(Guid messageId)
  {
    if (messageId == Guid.Empty)
      throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
  }
}
