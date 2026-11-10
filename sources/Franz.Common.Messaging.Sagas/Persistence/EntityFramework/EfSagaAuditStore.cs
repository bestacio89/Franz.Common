#nullable enable

using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// Enlists audit inserts in the active relational saga transaction.
/// </summary>
internal sealed class EfSagaAuditStore(SagaDbContext dbContext) : ISagaAuditStore
{
  private readonly SagaDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

  public Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    cancellationToken.ThrowIfCancellationRequested();

    if (_db.Database.CurrentTransaction is null)
      throw new InvalidOperationException(
          "Durable saga audit writes require the active SagaDbContext persistence transaction.");

    _db.Set<SagaAuditEntity>().Add(SagaAuditEntity.From(record));
    return Task.CompletedTask;
  }

  public async Task<int> DeleteExpiredAsync(
      DateTimeOffset cutoff,
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    if (batchSize <= 0)
      throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Audit retention batch size must be positive.");

    return await _db.Set<SagaAuditEntity>()
        .Where(record => record.Timestamp < cutoff.ToUniversalTime())
        .OrderBy(record => record.Timestamp)
        .ThenBy(record => record.AuditId)
        .Take(batchSize)
        .ExecuteDeleteAsync(cancellationToken)
        .ConfigureAwait(false);
  }
}
