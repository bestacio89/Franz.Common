#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

public sealed class MongoSagaMessageStore : IMessageStore
{
  private readonly IMongoCollection<MongoSagaStoredMessage> _outbox;
  private readonly IMongoCollection<MongoSagaStoredMessage> _deadLetters;
  private readonly MongoSagaUnitOfWork _unitOfWork;

  public MongoSagaMessageStore(IMongoDatabase database, MongoSagaUnitOfWork unitOfWork)
  {
    ArgumentNullException.ThrowIfNull(database);
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    _outbox = database.GetCollection<MongoSagaStoredMessage>("sagaOutbox");
    _deadLetters = database.GetCollection<MongoSagaStoredMessage>("sagaDeadLetters");
    _outbox.Indexes.CreateMany(new[]
    {
      new CreateIndexModel<MongoSagaStoredMessage>(Builders<MongoSagaStoredMessage>.IndexKeys.Ascending(item => item.SentOn)),
      new CreateIndexModel<MongoSagaStoredMessage>(Builders<MongoSagaStoredMessage>.IndexKeys.Ascending(item => item.NextAttemptAt))
    });
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var stored = MongoSagaStoredMessage.From(message.ToStored());
    if (_unitOfWork.CurrentSession is { } session)
      await _outbox.InsertOneAsync(session, stored, cancellationToken: cancellationToken).ConfigureAwait(false);
    else
      await _outbox.InsertOneAsync(stored, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    var filter = PendingFilter();
    var documents = _unitOfWork.CurrentSession is { } session
        ? await _outbox.Find(session, filter).SortBy(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false)
        : await _outbox.Find(filter).SortBy(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
    return documents.Select(item => item.ToStoredMessage()).ToArray();
  }

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
    var filter = PendingFilter();
    var documents = _unitOfWork.CurrentSession is { } session
        ? await _outbox.Find(session, filter).SortBy(item => item.Id).Limit(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false)
        : await _outbox.Find(filter).SortBy(item => item.Id).Limit(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);
    return documents.Select(item => item.ToStoredMessage()).ToArray();
  }

  public async Task<IReadOnlyList<StoredMessage>> ClaimPendingAsync(
      Guid owner, TimeSpan duration, int batchSize, CancellationToken ct = default)
  {
    ValidateClaim(owner, duration, batchSize);
    var now = DateTime.UtcNow;
    var filter = PendingFilter(now) &
                 (Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.LeaseExpiresAt, null) |
                  Builders<MongoSagaStoredMessage>.Filter.Lte(x => x.LeaseExpiresAt, now));
    var update = Builders<MongoSagaStoredMessage>.Update.Set(x => x.LeaseOwnerId, owner)
        .Set(x => x.LeaseExpiresAt, now.Add(duration));
    var options = new FindOneAndUpdateOptions<MongoSagaStoredMessage>
    {
      Sort = Builders<MongoSagaStoredMessage>.Sort.Ascending(x => x.Id),
      ReturnDocument = ReturnDocument.After
    };
    var claimed = new List<StoredMessage>(batchSize);
    for (var i = 0; i < batchSize; i++)
    {
      MongoSagaStoredMessage? doc;
      if (_unitOfWork.CurrentSession is { } session)
        doc = await _outbox.FindOneAndUpdateAsync(session, filter, update, options, ct).ConfigureAwait(false);
      else
        doc = await _outbox.FindOneAndUpdateAsync(filter, update, options, ct).ConfigureAwait(false);
      if (doc is null) break;
      claimed.Add(doc.ToStoredMessage());
    }
    return claimed;
  }

  public async Task MarkAsSentAsync(Guid id, Guid owner, CancellationToken ct = default)
  {
    var update = Builders<MongoSagaStoredMessage>.Update.Set(x => x.SentOn, DateTime.UtcNow)
        .Unset(x => x.LeaseOwnerId).Unset(x => x.LeaseExpiresAt);
    await EnsureOwnedUpdateAsync(id, owner, update, ct).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var update = Builders<MongoSagaStoredMessage>.Update.Set(x => x.RetryCount, message.RetryCount)
        .Set(x => x.LastError, message.LastError).Set(x => x.LastTriedOn, message.LastTriedOn)
        .Set(x => x.NextAttemptAt, message.NextAttemptAt)
        .Unset(x => x.LeaseOwnerId).Unset(x => x.LeaseExpiresAt);
    await EnsureOwnedUpdateAsync(message.Id, owner, update, ct).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    message.IsDeadLetter = true;
    message.LastTriedOn = DateTime.UtcNow;
    message.LeaseOwnerId = null;
    message.LeaseExpiresAt = null;
    var replacement = MongoSagaStoredMessage.From(message);
    var filter = Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.Id, message.Id) &
                 Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.LeaseOwnerId, owner) &
                 Builders<MongoSagaStoredMessage>.Filter.Gt(x => x.LeaseExpiresAt, DateTime.UtcNow);
    ReplaceOneResult replaced;
    if (_unitOfWork.CurrentSession is { } session)
      replaced = await _outbox.ReplaceOneAsync(session, filter, replacement, cancellationToken: ct).ConfigureAwait(false);
    else
      replaced = await _outbox.ReplaceOneAsync(filter, replacement, cancellationToken: ct).ConfigureAwait(false);
    if (replaced.MatchedCount != 1) throw LeaseLost(message.Id, owner);
    var byId = Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.Id, message.Id);
    if (_unitOfWork.CurrentSession is { } active)
    {
      await _deadLetters.ReplaceOneAsync(active, byId, replacement, new ReplaceOptions { IsUpsert = true }, ct).ConfigureAwait(false);
      await _outbox.DeleteOneAsync(active, byId, cancellationToken: ct).ConfigureAwait(false);
    }
    else
    {
      await _deadLetters.ReplaceOneAsync(byId, replacement, new ReplaceOptions { IsUpsert = true }, ct).ConfigureAwait(false);
      await _outbox.DeleteOneAsync(byId, ct).ConfigureAwait(false);
    }
  }

  private async Task EnsureOwnedUpdateAsync(Guid id, Guid owner, UpdateDefinition<MongoSagaStoredMessage> update, CancellationToken ct)
  {
    var filter = Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.Id, id) &
                 Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.LeaseOwnerId, owner) &
                 Builders<MongoSagaStoredMessage>.Filter.Gt(x => x.LeaseExpiresAt, DateTime.UtcNow);
    UpdateResult result;
    if (_unitOfWork.CurrentSession is { } session)
      result = await _outbox.UpdateOneAsync(session, filter, update, cancellationToken: ct).ConfigureAwait(false);
    else
      result = await _outbox.UpdateOneAsync(filter, update, cancellationToken: ct).ConfigureAwait(false);
    if (result.MatchedCount != 1) throw LeaseLost(id, owner);
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
    var update = Builders<MongoSagaStoredMessage>.Update.Set(item => item.SentOn, DateTime.UtcNow);
    var filter = Builders<MongoSagaStoredMessage>.Filter.Eq(item => item.Id, messageId);
    if (_unitOfWork.CurrentSession is { } session)
      await _outbox.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
    else
      await _outbox.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var update = Builders<MongoSagaStoredMessage>.Update
        .Set(item => item.RetryCount, message.RetryCount)
        .Set(item => item.LastError, message.LastError)
        .Set(item => item.LastTriedOn, message.LastTriedOn)
        .Set(item => item.NextAttemptAt, message.NextAttemptAt);
    var filter = Builders<MongoSagaStoredMessage>.Filter.Eq(item => item.Id, message.Id);
    if (_unitOfWork.CurrentSession is { } session)
      await _outbox.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
    else
      await _outbox.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    message.IsDeadLetter = true;
    var document = MongoSagaStoredMessage.From(message);
    var filter = Builders<MongoSagaStoredMessage>.Filter.Eq(item => item.Id, message.Id);

    if (_unitOfWork.CurrentSession is { } session)
    {
      await _deadLetters.ReplaceOneAsync(session, filter, document,
          new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
      await _outbox.DeleteOneAsync(session, filter, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    else
    {
      await _deadLetters.ReplaceOneAsync(filter, document,
          new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
      await _outbox.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    }
  }

  private static FilterDefinition<MongoSagaStoredMessage> PendingFilter(DateTime? now = null)
  {
    var current = now ?? DateTime.UtcNow;
    return Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.SentOn, null) &
           Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.IsDeadLetter, false) &
           (Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.NextAttemptAt, null) |
            Builders<MongoSagaStoredMessage>.Filter.Lte(x => x.NextAttemptAt, current)) &
           (Builders<MongoSagaStoredMessage>.Filter.Eq(x => x.LeaseExpiresAt, null) |
            Builders<MongoSagaStoredMessage>.Filter.Lte(x => x.LeaseExpiresAt, current));
  }
}
