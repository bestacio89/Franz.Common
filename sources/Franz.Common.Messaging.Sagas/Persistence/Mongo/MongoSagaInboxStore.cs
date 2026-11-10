#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

public sealed class MongoSagaInboxStore : IInboxStore
{
  private readonly IMongoCollection<MongoSagaInboxRecord> _collection;
  private readonly MongoSagaUnitOfWork _unitOfWork;

  public MongoSagaInboxStore(IMongoDatabase database, MongoSagaUnitOfWork unitOfWork)
  {
    ArgumentNullException.ThrowIfNull(database);
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    _collection = database.GetCollection<MongoSagaInboxRecord>("sagaInbox");
    // MessageId is the BSON _id field. MongoDB creates its unique _id index
    // automatically; creating a second unique index over the same field is invalid.
  }

  public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    Validate(messageId);
    var filter = Builders<MongoSagaInboxRecord>.Filter.Eq(record => record.MessageId, messageId);
    return _unitOfWork.CurrentSession is { } session
        ? _collection.Find(session, filter).AnyAsync(ct)
        : _collection.Find(filter).AnyAsync(ct);
  }

  public async Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
  {
    Validate(messageId);
    if (await HasProcessedAsync(messageId, ct).ConfigureAwait(false))
      return false;

    var record = new MongoSagaInboxRecord
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    };

    try
    {
      if (_unitOfWork.CurrentSession is { } session)
        await _collection.InsertOneAsync(session, record, cancellationToken: ct).ConfigureAwait(false);
      else
        await _collection.InsertOneAsync(record, cancellationToken: ct).ConfigureAwait(false);

      return true;
    }
    catch (MongoWriteException exception) when (
        exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
    {
      // A duplicate insert inside a transaction aborts that transaction. The
      // dispatcher must roll back and skip saga execution when registration loses.
      return false;
    }
  }

  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
      => _ = await TryRegisterAsync(messageId, ct).ConfigureAwait(false);

  private static void Validate(Guid messageId)
  {
    if (messageId == Guid.Empty)
      throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
  }
}
