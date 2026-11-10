#nullable enable

using System.Text.Json;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

public sealed class CosmosSagaMessageStore : IMessageStore
{
  private readonly CosmosSagaDbContext _db;
  private readonly CosmosSagaUnitOfWork _unitOfWork;

  public CosmosSagaMessageStore(CosmosSagaDbContext dbContext, CosmosSagaUnitOfWork unitOfWork)
  {
    _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
  }

  public Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    return SaveStoredAsync(message.ToStored(), cancellationToken);
  }

  public Task SaveScheduledAsync(
      Message message,
      DateTimeOffset notBefore,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var stored = message.ToStored();
    stored.NextAttemptAt = notBefore.UtcDateTime;
    return SaveStoredAsync(stored, cancellationToken);
  }

  private async Task SaveStoredAsync(StoredMessage stored, CancellationToken cancellationToken)
  {
    var tenantId = stored.Headers.TryGetValue(HeaderConstants.TenantId, out var values) &&
                   values.Length == 1
        ? values[0]
        : string.Empty;

    await _db.Set<CosmosSagaOutboxDocument>()
        .AddAsync(CosmosSagaOutboxDocument.From(stored, tenantId), cancellationToken)
        .ConfigureAwait(false);

    if (!_unitOfWork.IsActive)
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    var records = await PendingQuery().OrderBy(record => record.CreatedOn)
        .ToListAsync(cancellationToken).ConfigureAwait(false);
    return records.Select(record => record.ToStored()).ToArray();
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
    var records = await PendingQuery().OrderBy(record => record.CreatedOn)
        .Take(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);
    return records.Select(record => record.ToStored()).ToArray();
  }

  public async Task<IReadOnlyList<StoredMessage>> ClaimPendingAsync(
      Guid owner, TimeSpan duration, int batchSize, CancellationToken ct = default)
  {
    ValidateClaim(owner, duration, batchSize);
    var now = DateTime.UtcNow;
    var candidates = await PendingQuery(now).OrderBy(x => x.CreatedOn).Take(batchSize)
        .Select(x => new { x.Id, x.TenantId }).ToListAsync(ct).ConfigureAwait(false);
    var claimed = new List<StoredMessage>(candidates.Count);
    foreach (var candidate in candidates)
    {
      var record = await _db.Set<CosmosSagaOutboxDocument>()
          .SingleOrDefaultAsync(x => x.Id == candidate.Id && x.TenantId == candidate.TenantId, ct)
          .ConfigureAwait(false);
      if (record is null || !IsClaimable(record, now)) continue;
      var message = record.ToStored();
      message.LeaseOwnerId = owner;
      message.LeaseExpiresAt = now.Add(duration);
      record.UpdatePayload(message);
      try
      {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        claimed.Add(message);
      }
      catch (DbUpdateConcurrencyException)
      {
        _db.Entry(record).State = EntityState.Detached;
      }
    }
    return claimed;
  }

  public async Task MarkAsSentAsync(Guid id, Guid owner, CancellationToken ct = default)
  {
    var record = await OwnedRecordAsync(id, owner, ct).ConfigureAwait(false);
    var message = record.ToStored();
    message.SentOn = DateTime.UtcNow;
    Release(message);
    record.UpdatePayload(message);
    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var record = await OwnedRecordAsync(message.Id, owner, ct).ConfigureAwait(false);
    message.LeaseOwnerId = null;
    message.LeaseExpiresAt = null;
    record.UpdatePayload(message);
    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var record = await OwnedRecordAsync(message.Id, owner, ct).ConfigureAwait(false);
    message.IsDeadLetter = true;
    message.LastTriedOn = DateTime.UtcNow;
    Release(message);
    record.UpdatePayload(message);
    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  private async Task<CosmosSagaOutboxDocument> OwnedRecordAsync(Guid id, Guid owner, CancellationToken ct)
  {
    var record = await _db.Set<CosmosSagaOutboxDocument>()
        .SingleOrDefaultAsync(x => x.Id == id.ToString("N") && x.LeaseOwnerId == owner, ct)
        .ConfigureAwait(false);
    if (record is null || record.LeaseExpiresAt <= DateTime.UtcNow) throw LeaseLost(id, owner);
    return record;
  }

  private static bool IsClaimable(CosmosSagaOutboxDocument record, DateTime now)
      => record.SentOn is null && !record.IsDeadLetter &&
         (record.NextAttemptAt is null || record.NextAttemptAt <= now) &&
         (record.LeaseExpiresAt is null || record.LeaseExpiresAt <= now);

  private static void Release(StoredMessage message)
  {
    message.LeaseOwnerId = null;
    message.LeaseExpiresAt = null;
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
    var record = await FindByIdAsync(messageId, cancellationToken).ConfigureAwait(false);
    if (record is null)
      return;

    var stored = record.ToStored();
    stored.SentOn = DateTime.UtcNow;
    record.UpdatePayload(stored);
    if (!_unitOfWork.IsActive)
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var record = await FindByIdAsync(message.Id, cancellationToken).ConfigureAwait(false);
    if (record is null)
      return;

    record.UpdatePayload(message);
    if (!_unitOfWork.IsActive)
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var record = await FindByIdAsync(message.Id, cancellationToken).ConfigureAwait(false);
    if (record is null)
      return;

    message.IsDeadLetter = true;
    record.UpdatePayload(message);
    if (!_unitOfWork.IsActive)
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<CosmosSagaOutboxDocument> PendingQuery(DateTime? now = null)
  {
    var current = now ?? DateTime.UtcNow;
    return _db.Set<CosmosSagaOutboxDocument>().AsNoTracking()
        .Where(x => x.SentOn == null && !x.IsDeadLetter &&
                    (x.NextAttemptAt == null || x.NextAttemptAt <= current) &&
                    (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= current));
  }

  private Task<CosmosSagaOutboxDocument?> FindByIdAsync(Guid messageId, CancellationToken ct)
      => _db.Set<CosmosSagaOutboxDocument>()
          .SingleOrDefaultAsync(record => record.Id == messageId.ToString("N"), ct);
}
