#nullable enable

using Franz.Common.Messaging.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Repositories;

/// <summary>
/// MongoDB-backed inbox with a unique message identifier.
/// </summary>
public sealed class MongoInboxStore : IInboxStore
{
  private readonly IMongoCollection<ProcessedMessage> _collection;

  public MongoInboxStore(IMongoDatabase db)
  {
    ArgumentNullException.ThrowIfNull(db);

    _collection = db.GetCollection<ProcessedMessage>("ProcessedMessages");
    _collection.Indexes.CreateOne(
        new CreateIndexModel<ProcessedMessage>(
            Builders<ProcessedMessage>.IndexKeys.Ascending(x => x.MessageId),
            new CreateIndexOptions { Unique = true }));
  }

  public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);
    return _collection.Find(x => x.MessageId == messageId).AnyAsync(ct);
  }

  /// <summary>
  /// Uses the unique index as the concurrency primitive. No read-before-write
  /// check is performed, so concurrent duplicate deliveries cannot both win.
  /// </summary>
  public async Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);

    var processed = new ProcessedMessage
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    };

    try
    {
      await _collection.InsertOneAsync(
          processed,
          cancellationToken: ct).ConfigureAwait(false);
      return true;
    }
    catch (MongoWriteException exception) when (
        exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
    {
      return false;
    }
  }

  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    ValidateMessageId(messageId);

    var processed = new ProcessedMessage
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    };

    await _collection.InsertOneAsync(
        processed,
        cancellationToken: ct).ConfigureAwait(false);
  }

  private static void ValidateMessageId(Guid messageId)
  {
    if (messageId == Guid.Empty)
      throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
  }
}

public sealed class ProcessedMessage
{
  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid MessageId { get; set; }

  public DateTime ProcessedOn { get; set; }
}
