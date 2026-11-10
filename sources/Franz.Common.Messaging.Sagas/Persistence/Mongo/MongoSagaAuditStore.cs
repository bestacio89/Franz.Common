#nullable enable

using Franz.Common.Messaging.Sagas.Logging;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

/// <summary>
/// Persists audit entries in the same MongoDB session transaction as saga state, inbox, and outbox.
/// </summary>
internal sealed class MongoSagaAuditStore : ISagaAuditStore
{
  private readonly IMongoCollection<MongoSagaAuditDocument> _collection;
  private readonly MongoSagaUnitOfWork _unitOfWork;

  public MongoSagaAuditStore(IMongoDatabase database, MongoSagaUnitOfWork unitOfWork)
  {
    ArgumentNullException.ThrowIfNull(database);
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    _collection = database.GetCollection<MongoSagaAuditDocument>("sagaAudit");
  }

  public async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    var index = new CreateIndexModel<MongoSagaAuditDocument>(
        Builders<MongoSagaAuditDocument>.IndexKeys
            .Ascending(record => record.TimestampUtc)
            .Ascending(record => record.AuditId));
    await _collection.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task WriteAsync(SagaAuditRecord record, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(record);
    var session = _unitOfWork.CurrentSession;
    if (session?.IsInTransaction != true)
      throw new InvalidOperationException(
          "Durable saga audit writes require the active MongoDB saga transaction.");

    await _collection.InsertOneAsync(
        session,
        MongoSagaAuditDocument.From(record),
        cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task<int> DeleteExpiredAsync(
      DateTimeOffset cutoff,
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    if (batchSize <= 0)
      throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Audit retention batch size must be positive.");

    var filter = Builders<MongoSagaAuditDocument>.Filter.Lt(
        record => record.TimestampUtc,
        cutoff.UtcDateTime);
    var expiredIds = await _collection.Find(filter)
        .SortBy(record => record.TimestampUtc)
        .ThenBy(record => record.AuditId)
        .Limit(batchSize)
        .Project(record => record.AuditId)
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

    if (expiredIds.Count == 0)
      return 0;

    var result = await _collection.DeleteManyAsync(
        Builders<MongoSagaAuditDocument>.Filter.In(record => record.AuditId, expiredIds),
        cancellationToken).ConfigureAwait(false);

    return checked((int)result.DeletedCount);
  }
}
