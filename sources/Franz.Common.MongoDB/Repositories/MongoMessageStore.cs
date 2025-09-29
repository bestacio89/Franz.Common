using Franz.Common.Messaging;
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

    // Ensure indexes exist
    CreateIndexes();
  }

  private void CreateIndexes()
  {
    var indexModels = new List<CreateIndexModel<StoredMessage>>
        {
            // For fast lookups of unsent messages
            new CreateIndexModel<StoredMessage>(
                Builders<StoredMessage>.IndexKeys.Ascending(m => m.SentOn)),

            // For retry cycle handling
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
      => await _collection.Find(m => m.SentOn == null).ToListAsync(cancellationToken);

  public async Task MarkAsSentAsync(string id, CancellationToken cancellationToken = default)
  {
    var update = Builders<StoredMessage>.Update.Set(m => m.SentOn, DateTime.UtcNow);
    await _collection.UpdateOneAsync(m => m.Id == id, update, cancellationToken: cancellationToken);
  }


  public async Task UpdateRetryAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    var update = Builders<StoredMessage>.Update
        .Set(x => x.RetryCount, message.RetryCount)
        .Set(x => x.LastError, message.LastError)
        .Set(x => x.LastTriedOn, message.LastTriedOn);

    await _collection.UpdateOneAsync(
        x => x.Id == message.Id,
        update,
        cancellationToken: cancellationToken);
  }

  public async Task MoveToDeadLetterAsync(StoredMessage message, CancellationToken cancellationToken = default)
  {
    await _deadLetterCollection.InsertOneAsync(message, cancellationToken: cancellationToken);
    await _collection.DeleteOneAsync(m => m.Id == message.Id, cancellationToken);
  }
}
