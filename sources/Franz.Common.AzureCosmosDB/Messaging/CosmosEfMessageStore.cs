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
    var stored = message.ToStored();
    // CreatedOn, RetryCount etc. are initialized by StoredMessage itself
    await _dbContext.OutboxMessages.AddAsync(stored, cancellationToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    return await _dbContext.OutboxMessages
      .AsNoTracking()
      .Where(m => m.SentOn == null && !m.IsDeadLetter)
      .ToListAsync(cancellationToken);
  }

  public async Task MarkAsSentAsync(string messageId, CancellationToken cancellationToken = default)
  {
    var msg = await _dbContext.OutboxMessages
      .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    if (msg is null)
      return; // or throw if you want strict semantics

    msg.SentOn = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    // We assume 'message' is a deserialized copy from GetPendingAsync
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

    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
