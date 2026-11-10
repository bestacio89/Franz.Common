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
    return await PendingQuery().OrderBy(m => m.Id) // Explicitly sort by the sequential Guid
        .ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

    return await PendingQuery().OrderBy(m => m.Id)
        .Take(batchSize)
        .ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<StoredMessage>> ClaimPendingAsync(
      Guid owner, TimeSpan duration, int batchSize, CancellationToken ct = default)
  {
    ValidateClaim(owner, duration, batchSize);
    var now = DateTime.UtcNow;
    var ids = await PendingQuery(now).OrderBy(x => x.Id).Take(batchSize)
        .Select(x => x.Id).ToListAsync(ct).ConfigureAwait(false);
    var claimed = new List<StoredMessage>(ids.Count);
    foreach (var id in ids)
    {
      var item = await _dbContext.OutboxMessages.SingleOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
      if (item is null || !IsClaimable(item, now)) continue;
      item.LeaseOwnerId = owner;
      item.LeaseExpiresAt = now.Add(duration);
      try
      {
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        claimed.Add(item);
      }
      catch (DbUpdateConcurrencyException)
      {
        _dbContext.Entry(item).State = EntityState.Detached;
      }
    }
    return claimed;
  }

  public async Task MarkAsSentAsync(Guid id, Guid owner, CancellationToken ct = default)
  {
    var item = await OwnedAsync(id, owner, ct).ConfigureAwait(false);
    item.SentOn = DateTime.UtcNow;
    Release(item);
    await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var item = await OwnedAsync(message.Id, owner, ct).ConfigureAwait(false);
    item.RetryCount = message.RetryCount;
    item.LastError = message.LastError;
    item.LastTriedOn = message.LastTriedOn;
    item.NextAttemptAt = message.NextAttemptAt;
    Release(item);
    await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var item = await OwnedAsync(message.Id, owner, ct).ConfigureAwait(false);
    item.IsDeadLetter = true;
    item.LastError = message.LastError;
    item.LastTriedOn = DateTime.UtcNow;
    Release(item);
    await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  private async Task<StoredMessage> OwnedAsync(Guid id, Guid owner, CancellationToken ct)
  {
    var item = await _dbContext.OutboxMessages.SingleOrDefaultAsync(
        x => x.Id == id && x.LeaseOwnerId == owner, ct).ConfigureAwait(false);
    if (item is null || item.LeaseExpiresAt <= DateTime.UtcNow) throw LeaseLost(id, owner);
    return item;
  }

  private IQueryable<StoredMessage> PendingQuery(DateTime? now = null)
  {
    var current = now ?? DateTime.UtcNow;
    return _dbContext.OutboxMessages.AsNoTracking()
        .Where(x => x.SentOn == null && !x.IsDeadLetter &&
                    (x.NextAttemptAt == null || x.NextAttemptAt <= current) &&
                    (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= current));
  }

  private static bool IsClaimable(StoredMessage item, DateTime now)
      => item.SentOn is null && !item.IsDeadLetter &&
         (item.NextAttemptAt is null || item.NextAttemptAt <= now) &&
         (item.LeaseExpiresAt is null || item.LeaseExpiresAt <= now);

  private static void Release(StoredMessage item)
  {
    item.LeaseOwnerId = null;
    item.LeaseExpiresAt = null;
  }

  private static void ValidateClaim(Guid owner, TimeSpan duration, int batchSize)
  {
    if (owner == Guid.Empty) throw new ArgumentException("Lease owner cannot be empty.", nameof(owner));
    if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
  }

  private static InvalidOperationException LeaseLost(Guid id, Guid owner)
      => new($"Outbox lease for message '{id}' is no longer owned by dispatcher '{owner}'.");

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
    existing.NextAttemptAt = message.NextAttemptAt;

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