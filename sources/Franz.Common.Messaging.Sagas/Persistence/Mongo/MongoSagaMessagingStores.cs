#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

internal sealed class MongoSagaInboxRecord
{
  [BsonId]
  [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
  public Guid MessageId { get; set; }
  public DateTime ProcessedOn { get; set; }
}

/// <summary>Mongo inbox adapter enlisted in MongoSagaUnitOfWork's session.</summary>
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
      // The unique _id index is the concurrency arbiter. A duplicate insert inside
      // a transaction aborts that transaction; the dispatcher observes false and
      // rolls the unit of work back without executing saga handlers.
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

/// <summary>Mongo outbox adapter enlisted in MongoSagaUnitOfWork's session.</summary>
public sealed class MongoSagaMessageStore : IMessageStore
{
  private readonly IMongoCollection<StoredMessage> _outbox;
  private readonly IMongoCollection<StoredMessage> _deadLetters;
  private readonly MongoSagaUnitOfWork _unitOfWork;

  public MongoSagaMessageStore(IMongoDatabase database, MongoSagaUnitOfWork unitOfWork)
  {
    ArgumentNullException.ThrowIfNull(database);
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    _outbox = database.GetCollection<StoredMessage>("sagaOutbox");
    _deadLetters = database.GetCollection<StoredMessage>("sagaDeadLetters");
    _outbox.Indexes.CreateMany(new[]
    {
      new CreateIndexModel<StoredMessage>(Builders<StoredMessage>.IndexKeys.Ascending(item => item.SentOn)),
      new CreateIndexModel<StoredMessage>(Builders<StoredMessage>.IndexKeys.Ascending(item => item.NextAttemptAt))
    });
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var stored = message.ToStored();
    if (_unitOfWork.CurrentSession is { } session)
      await _outbox.InsertOneAsync(session, stored, cancellationToken: cancellationToken).ConfigureAwait(false);
    else
      await _outbox.InsertOneAsync(stored, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    var filter = PendingFilter();
    return _unitOfWork.CurrentSession is { } session
        ? await _outbox.Find(session, filter).SortBy(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false)
        : await _outbox.Find(filter).SortBy(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
    var filter = PendingFilter();
    return _unitOfWork.CurrentSession is { } session
        ? await _outbox.Find(session, filter).SortBy(item => item.Id).Limit(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false)
        : await _outbox.Find(filter).SortBy(item => item.Id).Limit(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default)
  {
    var update = Builders<StoredMessage>.Update.Set(item => item.SentOn, DateTime.UtcNow);
    var filter = Builders<StoredMessage>.Filter.Eq(item => item.Id, messageId);
    if (_unitOfWork.CurrentSession is { } session)
      await _outbox.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
    else
      await _outbox.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var update = Builders<StoredMessage>.Update
        .Set(item => item.RetryCount, message.RetryCount)
        .Set(item => item.LastError, message.LastError)
        .Set(item => item.LastTriedOn, message.LastTriedOn)
        .Set(item => item.NextAttemptAt, message.NextAttemptAt);
    var filter = Builders<StoredMessage>.Filter.Eq(item => item.Id, message.Id);
    if (_unitOfWork.CurrentSession is { } session)
      await _outbox.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
    else
      await _outbox.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    message.IsDeadLetter = true;
    var filter = Builders<StoredMessage>.Filter.Eq(item => item.Id, message.Id);

    if (_unitOfWork.CurrentSession is { } session)
    {
      await _deadLetters.ReplaceOneAsync(session, filter, message,
          new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
      await _outbox.DeleteOneAsync(session, filter, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    else
    {
      await _deadLetters.ReplaceOneAsync(filter, message,
          new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
      await _outbox.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    }
  }

  private static FilterDefinition<StoredMessage> PendingFilter()
      => Builders<StoredMessage>.Filter.Eq(item => item.SentOn, null) &
         Builders<StoredMessage>.Filter.Eq(item => item.IsDeadLetter, false) &
         (Builders<StoredMessage>.Filter.Eq(item => item.NextAttemptAt, null) |
          Builders<StoredMessage>.Filter.Lte(item => item.NextAttemptAt, DateTime.UtcNow));
}
