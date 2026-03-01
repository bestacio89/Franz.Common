#nullable enable
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Messaging;

public class CosmosEfMessageStore : IMessageStore
{
  private readonly CosmosMessagingDbContext _dbContext;

  public CosmosEfMessageStore(CosmosMessagingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    // ToStored() now correctly maps the native Guid v7 IDs
    var stored = message.ToStored();

    await _dbContext.OutboxMessages.AddAsync(stored, cancellationToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    // Guid v7 ensures that the oldest messages are physically 'earlier' in many 
    // indexing scenarios, helping with FIFO-ish processing.
    return await _dbContext.OutboxMessages
        .AsNoTracking()
        .Where(m => m.SentOn == null && !m.IsDeadLetter)
        .OrderBy(m => m.Id) // Explicitly sort by the sequential Guid
        .ToListAsync(cancellationToken);
  }

  // 🛠️ FIX: Changed messageId parameter type from string to Guid
  public async Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default)
  {
    var msg = await _dbContext.OutboxMessages
        .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    if (msg is null)
      return;

    msg.SentOn = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    // StoredMessage.Id is now a Guid, so EF handles the comparison natively
    var existing = await _dbContext.OutboxMessages
        .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

    if (existing is null)
      return;

    existing.RetryCount = message.RetryCount;
    existing.LastError = message.LastError;
    existing.LastTriedOn = message.LastTriedOn;

    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.OutboxMessages
        .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

    if (existing is null)
      return;

    existing.IsDeadLetter = true;
    existing.LastTriedOn = DateTime.UtcNow;
    existing.LastError = message.LastError; // Preserve the error that killed it

    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}