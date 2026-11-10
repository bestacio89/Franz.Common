#nullable enable

using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Enlists audit documents in the active tenant-partitioned Cosmos transactional batch.
/// </summary>
internal sealed class CosmosSagaAuditStore : ISagaAuditStore
{
  private readonly CosmosSagaDbContext _db;
  private readonly CosmosSagaUnitOfWork _unitOfWork;

  public CosmosSagaAuditStore(CosmosSagaDbContext db, CosmosSagaUnitOfWork unitOfWork)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
  }

  public async Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    if (!_unitOfWork.IsActive)
      throw new InvalidOperationException(
          "Durable saga audit writes require the active Cosmos saga persistence unit.");

    var document = new CosmosSagaAuditDocument(record.AuditId)
    {
      TenantId = record.TenantId ?? string.Empty,
      SagaId = record.SagaId,
      SagaType = record.SagaType,
      StateType = record.StateType,
      StepType = record.StepType,
      IncomingMessageType = record.IncomingMessageType,
      OutgoingMessageType = record.OutgoingMessageType,
      CorrelationId = record.CorrelationId,
      CausationId = record.CausationId,
      MessageId = record.MessageId,
      Status = (int)record.Status,
      Timestamp = record.Timestamp.ToUniversalTime(),
      DurationTicks = record.Duration.Ticks,
      ErrorType = record.ErrorType
    };
    document.MarkCreated("saga-audit");
    await _db.Set<CosmosSagaAuditDocument>().AddAsync(document, cancellationToken).ConfigureAwait(false);
  }

  public async Task<int> DeleteExpiredAsync(
      DateTimeOffset cutoff,
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    if (batchSize <= 0)
      throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Audit retention batch size must be positive.");
    if (_unitOfWork.IsActive)
      throw new InvalidOperationException("Saga audit retention cannot run inside a saga execution transaction.");

    // Cosmos transactional batches are limited to 100 operations per logical partition.
    var effectiveBatchSize = Math.Min(batchSize, 100);
    var expired = await _db.Set<CosmosSagaAuditDocument>()
        .Where(record => record.Timestamp < cutoff.ToUniversalTime())
        .OrderBy(record => record.Timestamp)
        .Take(effectiveBatchSize)
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

    foreach (var partition in expired.GroupBy(record => record.TenantId, StringComparer.Ordinal))
    {
      _db.Set<CosmosSagaAuditDocument>().RemoveRange(partition);
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    return expired.Count;
  }
}
