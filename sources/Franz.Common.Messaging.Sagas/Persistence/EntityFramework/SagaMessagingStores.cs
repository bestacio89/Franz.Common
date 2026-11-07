#nullable enable

using System.Text.Json;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

internal sealed class SagaInboxRecord
{
  public Guid MessageId { get; set; }
  public DateTime ProcessedOn { get; set; }
}

internal sealed class SagaOutboxRecord
{
  public Guid Id { get; set; }
  public string Body { get; set; } = string.Empty;
  public string HeadersJson { get; set; } = "{}";
  public string PropertiesJson { get; set; } = "{}";
  public Guid? CorrelationId { get; set; }
  public string? MessageType { get; set; }
  public DateTime CreatedOn { get; set; }
  public DateTime? SentOn { get; set; }
  public int RetryCount { get; set; }
  public string? LastError { get; set; }
  public DateTime? LastTriedOn { get; set; }
  public DateTime? NextAttemptAt { get; set; }
  public bool IsDeadLetter { get; set; }

  public static SagaOutboxRecord From(Message message)
  {
    var stored = message.ToStored();
    return new SagaOutboxRecord
    {
      Id = stored.Id,
      Body = stored.Body,
      HeadersJson = JsonSerializer.Serialize(stored.Headers),
      PropertiesJson = JsonSerializer.Serialize(stored.Properties),
      CorrelationId = stored.CorrelationId,
      MessageType = stored.MessageType,
      CreatedOn = stored.CreatedOn
    };
  }

  public StoredMessage ToStored()
  {
    var headers = JsonSerializer.Deserialize<Dictionary<string, string[]>>(HeadersJson)
        ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(PropertiesJson)
        ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    return new StoredMessage
    {
      Id = Id,
      Body = Body,
      Headers = headers,
      Properties = properties.ToDictionary(
          pair => pair.Key,
          pair => (object)pair.Value,
          StringComparer.OrdinalIgnoreCase),
      CorrelationId = CorrelationId,
      MessageType = MessageType,
      CreatedOn = CreatedOn,
      SentOn = SentOn,
      RetryCount = RetryCount,
      LastError = LastError,
      LastTriedOn = LastTriedOn,
      NextAttemptAt = NextAttemptAt,
      IsDeadLetter = IsDeadLetter
    };
  }
}

/// <summary>
/// EF inbox adapter sharing SagaDbContext and its active IUnitOfWork transaction.
/// </summary>
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

/// <summary>
/// EF outbox adapter sharing SagaDbContext and its active IUnitOfWork transaction.
/// </summary>
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

  private IQueryable<SagaOutboxRecord> PendingQuery()
      => _db.Set<SagaOutboxRecord>().AsNoTracking()
          .Where(record => record.SentOn == null &&
                           !record.IsDeadLetter &&
                           (record.NextAttemptAt == null || record.NextAttemptAt <= DateTime.UtcNow));
}
