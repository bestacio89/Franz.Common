#nullable enable
using Franz.Common.Messaging.Storage;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Repositories;

public class MongoInboxStore : IInboxStore
{
  private readonly IMongoCollection<ProcessedMessage> _collection;

  public MongoInboxStore(IMongoDatabase db)
  {
    _collection = db.GetCollection<ProcessedMessage>("ProcessedMessages");

    // ✅ UNIQUE INDEX: Using Guid v7 here is elite.
    // It ensures no two messages with the same ID are processed,
    // while keeping the index inserts sequential and fast.
    _collection.Indexes.CreateOne(
        new CreateIndexModel<ProcessedMessage>(
            Builders<ProcessedMessage>.IndexKeys.Ascending(x => x.MessageId),
            new CreateIndexOptions { Unique = true }));
  }

  // 🛠️ FIX: Changed parameter from string to Guid
  public async Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
      => await _collection.Find(x => x.MessageId == messageId).AnyAsync(ct);

  // 🛠️ FIX: Changed parameter from string to Guid
  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    var processed = new ProcessedMessage
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    };

    // If a duplicate arrives, the unique index will throw a MongoWriteException,
    // which is exactly what we want to prevent double-processing.
    await _collection.InsertOneAsync(processed, cancellationToken: ct);
  }
}

public class ProcessedMessage
{
  // 🛠️ FIX: MessageId is now a native Guid
  public Guid MessageId { get; set; }
  public DateTime ProcessedOn { get; set; }
}