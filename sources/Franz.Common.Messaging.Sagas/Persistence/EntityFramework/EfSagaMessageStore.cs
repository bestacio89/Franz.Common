#nullable enable

using System.Text.Json;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

public sealed class EfSagaMessageStore(SagaDbContext dbContext) : IMessageStore
{
  private readonly SagaDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    await _db.Set<SagaOutboxRecord>()
        .AddAsync(SagaOutboxRecord.From(message), cancellationToken)
        .ConfigureAwait(false);
    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    var records = await PendingQuery()
        .OrderBy(record => record.Id)
        .ToListAsync(cancellationToken).ConfigureAwait(false);
    return records.Select(record => record.ToStored()).ToArray();
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
    var records = await PendingQuery()
        .OrderBy(record => record.Id)
        .Take(batchSize)
        .ToListAsync(cancellationToken).ConfigureAwait(false);
    return records.Select(record => record.ToStored()).ToArray();
  }

  public async Task<IReadOnlyList<StoredMessage>> ClaimPendingAsync(
      Guid owner, TimeSpan duration, int batchSize, CancellationToken ct = default)
  {
    ValidateClaim(owner, duration, batchSize);
    var now = DateTime.UtcNow;
    var ids = await PendingQuery(now).OrderBy(x => x.Id).Take(batchSize)
        .Select(x => x.Id).ToListAsync(ct).ConfigureAwait(false);
    foreach (var id in ids)
    {
      await _db.Set<SagaOutboxRecord>()
          .Where(x => x.Id == id && x.SentOn == null && !x.IsDeadLetter &&
                      (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
                      (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= now))
          .ExecuteUpdateAsync(update => update
              .SetProperty(x => x.LeaseOwnerId, (Guid?)owner)
              .SetProperty(x => x.LeaseExpiresAt, (DateTime?)now.Add(duration)), ct)
          .ConfigureAwait(false);
    }
    var records = await _db.Set<SagaOutboxRecord>().AsNoTracking()
        .Where(x => x.LeaseOwnerId == owner && x.LeaseExpiresAt > now)
        .OrderBy(x => x.Id).ToListAsync(ct).ConfigureAwait(false);
    return records.Select(x => x.ToStored()).ToArray();
  }

  public async Task MarkAsSentAsync(Guid id, Guid owner, CancellationToken ct = default)
      => await UpdateOwnedAsync(id, owner, update => update
          .SetProperty(x => x.SentOn, (DateTime?)DateTime.UtcNow)
          .SetProperty(x => x.LeaseOwnerId, (Guid?)null)
          .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), ct).ConfigureAwait(false);

  public async Task UpdateRetryAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    await UpdateOwnedAsync(message.Id, owner, update => update
        .SetProperty(x => x.RetryCount, message.RetryCount)
        .SetProperty(x => x.LastError, message.LastError)
        .SetProperty(x => x.LastTriedOn, message.LastTriedOn)
        .SetProperty(x => x.NextAttemptAt, message.NextAttemptAt)
        .SetProperty(x => x.LeaseOwnerId, (Guid?)null)
        .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), ct).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    await UpdateOwnedAsync(message.Id, owner, update => update
        .SetProperty(x => x.IsDeadLetter, true)
        .SetProperty(x => x.LastError, message.LastError)
        .SetProperty(x => x.LastTriedOn, DateTime.UtcNow)
        .SetProperty(x => x.LeaseOwnerId, (Guid?)null)
        .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), ct).ConfigureAwait(false);
  }

  private async Task UpdateOwnedAsync(
      Guid id, Guid owner,
      Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<SagaOutboxRecord>,
           Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<SagaOutboxRecord>> update,
      CancellationToken ct)
  {
    var affected = await _db.Set<SagaOutboxRecord>()
        .Where(x => x.Id == id && x.LeaseOwnerId == owner && x.LeaseExpiresAt > DateTime.UtcNow)
        .ExecuteUpdateAsync(update, ct).ConfigureAwait(false);
    if (affected != 1) throw LeaseLost(id, owner);
  }

  private static void ValidateClaim(Guid owner, TimeSpan duration, int batchSize)
  {
    if (owner == Guid.Empty) throw new ArgumentException("Lease owner cannot be empty.", nameof(owner));
    if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
  }

  private static InvalidOperationException LeaseLost(Guid id, Guid owner)
      => new($"Outbox lease for message '{id}' is no longer owned by dispatcher '{owner}'.");

  public async Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default)
  {
    var record = await _db.Set<SagaOutboxRecord>()
        .SingleOrDefaultAsync(item => item.Id == messageId, cancellationToken)
        .ConfigureAwait(false);
    if (record is null)
      return;

    record.SentOn = DateTime.UtcNow;
    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var record = await _db.Set<SagaOutboxRecord>()
        .SingleOrDefaultAsync(item => item.Id == message.Id, cancellationToken)
        .ConfigureAwait(false);
    if (record is null)
      return;

    record.RetryCount = message.RetryCount;
    record.LastError = message.LastError;
    record.LastTriedOn = message.LastTriedOn;
    record.NextAttemptAt = message.NextAttemptAt;
    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var record = await _db.Set<SagaOutboxRecord>()
        .SingleOrDefaultAsync(item => item.Id == message.Id, cancellationToken)
        .ConfigureAwait(false);
    if (record is null)
      return;

    record.IsDeadLetter = true;
    record.LastError = message.LastError;
    record.LastTriedOn = DateTime.UtcNow;
    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<SagaOutboxRecord> PendingQuery(DateTime? now = null)
  {
    var current = now ?? DateTime.UtcNow;
    return _db.Set<SagaOutboxRecord>().AsNoTracking()
        .Where(x => x.SentOn == null && !x.IsDeadLetter &&
                    (x.NextAttemptAt == null || x.NextAttemptAt <= current) &&
                    (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= current));
  }
}
