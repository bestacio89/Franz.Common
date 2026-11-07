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

internal sealed class CosmosSagaInboxDocument
{
  public string Id { get; set; } = default!;
  public string TenantId { get; set; } = string.Empty;
  public DateTime ProcessedOn { get; set; }
}

internal sealed class CosmosSagaOutboxDocument
{
  public string Id { get; set; } = default!;
  public string TenantId { get; set; } = string.Empty;
  public string Payload { get; set; } = default!;
  public DateTime CreatedOn { get; set; }
  public DateTime? SentOn { get; set; }
  public DateTime? NextAttemptAt { get; set; }
  public bool IsDeadLetter { get; set; }

  public StoredMessage ToStored()
      => JsonSerializer.Deserialize<StoredMessage>(Payload)
          ?? throw new InvalidOperationException($"Cosmos outbox document '{Id}' has an invalid payload.");

  public static CosmosSagaOutboxDocument From(StoredMessage message, string tenantId)
      => new()
      {
        Id = message.Id.ToString("N"),
        TenantId = tenantId,
        Payload = JsonSerializer.Serialize(message),
        CreatedOn = message.CreatedOn,
        SentOn = message.SentOn,
        NextAttemptAt = message.NextAttemptAt,
        IsDeadLetter = message.IsDeadLetter
      };

  public void UpdatePayload(StoredMessage message)
  {
    Payload = JsonSerializer.Serialize(message);
    SentOn = message.SentOn;
    NextAttemptAt = message.NextAttemptAt;
    IsDeadLetter = message.IsDeadLetter;
  }
}

/// <summary>Cosmos inbox adapter staged in the same transactional batch as saga state.</summary>
public sealed class CosmosSagaInboxStore : IInboxStore
{
  private readonly CosmosSagaDbContext _db;
  private readonly CosmosSagaUnitOfWork _unitOfWork;

  public CosmosSagaInboxStore(CosmosSagaDbContext dbContext, CosmosSagaUnitOfWork unitOfWork)
  {
    _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
  }

  public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    Validate(messageId);
    var tenantId = CurrentTenantId();
    return _db.Set<CosmosSagaInboxDocument>().AsNoTracking()
        .AnyAsync(record => record.Id == messageId.ToString("N") && record.TenantId == tenantId, ct);
  }

  public async Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
  {
    Validate(messageId);
    var tenantId = CurrentTenantId();

    if (await _db.Set<CosmosSagaInboxDocument>().AnyAsync(
        record => record.Id == messageId.ToString("N") && record.TenantId == tenantId, ct)
        .ConfigureAwait(false))
      return false;

    await _db.Set<CosmosSagaInboxDocument>().AddAsync(new CosmosSagaInboxDocument
    {
      Id = messageId.ToString("N"),
      TenantId = tenantId,
      ProcessedOn = DateTime.UtcNow
    }, ct).ConfigureAwait(false);

    if (!_unitOfWork.IsActive)
      await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    return true;
  }

  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
      => _ = await TryRegisterAsync(messageId, ct).ConfigureAwait(false);

  private static string CurrentTenantId()
      => MediatorContext.TenantId ?? string.Empty;

  private static void Validate(Guid messageId)
  {
    if (messageId == Guid.Empty)
      throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
  }
}

/// <summary>Cosmos outbox adapter staged in the same transactional batch as saga state.</summary>
public sealed class CosmosSagaMessageStore : IMessageStore
{
  private readonly CosmosSagaDbContext _db;
  private readonly CosmosSagaUnitOfWork _unitOfWork;

  public CosmosSagaMessageStore(CosmosSagaDbContext dbContext, CosmosSagaUnitOfWork unitOfWork)
  {
    _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var stored = message.ToStored();
    var tenantId = message.Headers.TryGetValue(HeaderConstants.TenantId, out var values) &&
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

  private IQueryable<CosmosSagaOutboxDocument> PendingQuery()
      => _db.Set<CosmosSagaOutboxDocument>().AsNoTracking()
          .Where(record => record.SentOn == null &&
                           !record.IsDeadLetter &&
                           (record.NextAttemptAt == null || record.NextAttemptAt <= DateTime.UtcNow));

  private Task<CosmosSagaOutboxDocument?> FindByIdAsync(Guid messageId, CancellationToken ct)
      => _db.Set<CosmosSagaOutboxDocument>()
          .SingleOrDefaultAsync(record => record.Id == messageId.ToString("N"), ct);
}
