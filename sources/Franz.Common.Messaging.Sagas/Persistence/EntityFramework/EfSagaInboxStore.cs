#nullable enable

using System.Text.Json;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

public sealed class EfSagaInboxStore(SagaDbContext dbContext) : IInboxStore
{
  private readonly SagaDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

  public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);
    return _db.Set<SagaInboxRecord>().AsNoTracking()
        .AnyAsync(record => record.MessageId == messageId, ct);
  }

  public async Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);

    if (await HasProcessedAsync(messageId, ct).ConfigureAwait(false))
      return false;

    await _db.Set<SagaInboxRecord>().AddAsync(new SagaInboxRecord
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    }, ct).ConfigureAwait(false);

    // The unique primary key arbitrates concurrent deliveries. A conflicting insert
    // fails the enclosing transaction; the dispatcher rolls it back and lets broker
    // redelivery observe the committed receipt.
    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    return true;
  }

  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    _ = await TryRegisterAsync(messageId, ct).ConfigureAwait(false);
  }

  private static void ValidateMessageId(Guid messageId)
  {
    if (messageId == Guid.Empty)
      throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
  }
}
