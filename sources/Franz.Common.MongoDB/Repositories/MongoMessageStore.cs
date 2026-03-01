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

            // GUID V7 Index: Naturally handles chronological ordering 
            // and provides high-performance unique lookups.
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.Id)),

            // For retry cycle handling and DLQ triage
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.RetryCount)),

            // For cleanup / archival jobs
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.CreatedOn))
        };

    _collection.Indexes.CreateMany(indexModels);
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
      => await _collection.InsertOneAsync(message.ToStored(), cancellationToken: cancellationToken);

  public async Task<IReadOnlyList<StoredMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
      => await _collection.Find(m => m.SentOn == null && !m.IsDeadLetter)
                          .SortBy(m => m.Id) // Leverage Guid v7 sequentiality for FIFO
                          .ToListAsync(cancellationToken);

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
        .Set(x => x.LastTriedOn, message.LastTriedOn);

    await _collection.UpdateOneAsync(
        x => x.Id == message.Id, // message.Id is now a Guid
        update,
        cancellationToken: cancellationToken);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    // Tag as DLQ before moving
    message.IsDeadLetter = true;

    await _deadLetterCollection.InsertOneAsync(message, cancellationToken: cancellationToken);
    await _collection.DeleteOneAsync(m => m.Id == message.Id, cancellationToken);
  }
}