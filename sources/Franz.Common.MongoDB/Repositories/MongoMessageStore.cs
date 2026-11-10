#nullable enable
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Repositories;

public class MongoMessageStore : IMessageStore
{
  private readonly IMongoCollection<StoredMessage> _collection;
  private readonly IMongoCollection<StoredMessage> _deadLetterCollection;

  public MongoMessageStore(IMongoDatabase database,
                           string outboxCollectionName = "OutboxMessages",
                           string deadLetterCollectionName = "DeadLetterMessages")
  {
    _collection = database.GetCollection<StoredMessage>(outboxCollectionName);
    _deadLetterCollection = database.GetCollection<StoredMessage>(deadLetterCollectionName);

    // Ensure indexes exist for high-volume Outbox polling
    CreateIndexes();
  }

  private void CreateIndexes()
  {
    var indexModels = new List<CreateIndexModel<StoredMessage>>
        {
            // For fast lookups of unsent messages (The most common query)
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.SentOn)),

            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.NextAttemptAt)),

            // For retry cycle handling and DLQ triage
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.RetryCount)),

            // For cleanup / archival jobs
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.CreatedOn))
        };

    _collection.Indexes.CreateMany(indexModels);

    // MongoDB creates a unique index for the document _id automatically.
    // StoredMessage.Id is mapped to _id by the driver's Id convention, so
    // creating another index on Id is both redundant and rejected by MongoDB.
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
      => await _collection.InsertOneAsync(message.ToStored(), cancellationToken: cancellationToken);

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
      => await _collection.Find(PendingFilter())
                          .SortBy(m => m.Id) // Leverage Guid v7 sequentiality for FIFO
                          .ToListAsync(cancellationToken);

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(
      int batchSize,
      CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

    return await _collection.Find(PendingFilter())
        .SortBy(m => m.Id)
        .Limit(batchSize)
        .ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<StoredMessage>> ClaimPendingAsync(
      Guid owner, TimeSpan duration, int batchSize, CancellationToken ct = default)
  {
    if (owner == Guid.Empty) throw new ArgumentException("Lease owner cannot be empty.", nameof(owner));
    if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
    var now = DateTime.UtcNow;
    var filter = PendingFilter(now);
    var update = Builders<StoredMessage>.Update.Set(x => x.LeaseOwnerId, owner)
        .Set(x => x.LeaseExpiresAt, now.Add(duration));
    var options = new FindOneAndUpdateOptions<StoredMessage>
    {
      Sort = Builders<StoredMessage>.Sort.Ascending(x => x.Id),
      ReturnDocument = ReturnDocument.After
    };
    var claimed = new List<StoredMessage>(batchSize);
    for (var i = 0; i < batchSize; i++)
    {
      var item = await _collection.FindOneAndUpdateAsync(filter, update, options, ct).ConfigureAwait(false);
      if (item is null) break;
      claimed.Add(item);
    }
    return claimed;
  }

  public async Task MarkAsSentAsync(Guid id, Guid owner, CancellationToken ct = default)
  {
    var update = Builders<StoredMessage>.Update.Set(x => x.SentOn, DateTime.UtcNow)
        .Unset(x => x.LeaseOwnerId).Unset(x => x.LeaseExpiresAt);
    await EnsureOwnedUpdateAsync(id, owner, update, ct).ConfigureAwait(false);
  }

  public async Task UpdateRetryAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var update = Builders<StoredMessage>.Update.Set(x => x.RetryCount, message.RetryCount)
        .Set(x => x.LastError, message.LastError).Set(x => x.LastTriedOn, message.LastTriedOn)
        .Set(x => x.NextAttemptAt, message.NextAttemptAt)
        .Unset(x => x.LeaseOwnerId).Unset(x => x.LeaseExpiresAt);
    await EnsureOwnedUpdateAsync(message.Id, owner, update, ct).ConfigureAwait(false);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, Guid owner, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    var filter = Builders<StoredMessage>.Filter.Eq(x => x.Id, message.Id) &
                 Builders<StoredMessage>.Filter.Eq(x => x.LeaseOwnerId, owner) &
                 Builders<StoredMessage>.Filter.Gt(x => x.LeaseExpiresAt, DateTime.UtcNow);
    message.IsDeadLetter = true;
    message.LastTriedOn = DateTime.UtcNow;
    message.LeaseOwnerId = null;
    message.LeaseExpiresAt = null;
    var result = await _collection.ReplaceOneAsync(filter, message, cancellationToken: ct).ConfigureAwait(false);
    if (result.MatchedCount != 1) throw LeaseLost(message.Id, owner);
    await _deadLetterCollection.ReplaceOneAsync(x => x.Id == message.Id, message,
        new ReplaceOptions { IsUpsert = true }, ct).ConfigureAwait(false);
    await _collection.DeleteOneAsync(x => x.Id == message.Id, ct).ConfigureAwait(false);
  }

  private async Task EnsureOwnedUpdateAsync(Guid id, Guid owner, UpdateDefinition<StoredMessage> update, CancellationToken ct)
  {
    var filter = Builders<StoredMessage>.Filter.Eq(x => x.Id, id) &
                 Builders<StoredMessage>.Filter.Eq(x => x.LeaseOwnerId, owner) &
                 Builders<StoredMessage>.Filter.Gt(x => x.LeaseExpiresAt, DateTime.UtcNow);
    var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct).ConfigureAwait(false);
    if (result.MatchedCount != 1) throw LeaseLost(id, owner);
  }

  private static FilterDefinition<StoredMessage> PendingFilter(DateTime? now = null)
  {
    var current = now ?? DateTime.UtcNow;
    return Builders<StoredMessage>.Filter.Eq(x => x.SentOn, null) &
           Builders<StoredMessage>.Filter.Eq(x => x.IsDeadLetter, false) &
           (Builders<StoredMessage>.Filter.Eq(x => x.NextAttemptAt, null) |
            Builders<StoredMessage>.Filter.Lte(x => x.NextAttemptAt, current)) &
           (Builders<StoredMessage>.Filter.Eq(x => x.LeaseExpiresAt, null) |
            Builders<StoredMessage>.Filter.Lte(x => x.LeaseExpiresAt, current));
  }

  private static InvalidOperationException LeaseLost(Guid id, Guid owner)
      => new($"Outbox lease for message '{id}' is no longer owned by dispatcher '{owner}'.");

  // 🛠️ FIX: Changed parameter type from string to Guid to match IMessageStore
  public async Task MarkAsSentAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var update = Builders<StoredMessage>.Update.Set(m => m.SentOn, DateTime.UtcNow);

    // MongoDB driver handles Guid types natively if configured (usually via BsonSerializer)
    await _collection.UpdateOneAsync(m => m.Id == id, update, cancellationToken: cancellationToken);
  }

  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    var update = Builders<StoredMessage>.Update
        .Set(x => x.RetryCount, message.RetryCount)
        .Set(x => x.LastError, message.LastError)
        .Set(x => x.LastTriedOn, message.LastTriedOn)
        .Set(x => x.NextAttemptAt, message.NextAttemptAt);

    await _collection.UpdateOneAsync(
        x => x.Id == message.Id, // message.Id is now a Guid
        update,
        cancellationToken: cancellationToken);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    // Upsert by stable message ID so a retry after a crash between the DLQ write
    // and outbox deletion cannot create duplicate dead-letter records.
    message.IsDeadLetter = true;

    await _deadLetterCollection.ReplaceOneAsync(
        m => m.Id == message.Id,
        message,
        new ReplaceOptions { IsUpsert = true },
        cancellationToken);

    await _collection.DeleteOneAsync(m => m.Id == message.Id, cancellationToken);
  }
}