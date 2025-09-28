using Franz.Common.Messaging.Storage;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.MongoDB.Repositories;
public class MongoInboxStore : IInboxStore
{
  private readonly IMongoCollection<ProcessedMessage> _collection;

  public MongoInboxStore(IMongoDatabase db)
  {
    _collection = db.GetCollection<ProcessedMessage>("ProcessedMessages");

    // unique index on MessageId
    _collection.Indexes.CreateOne(
        new CreateIndexModel<ProcessedMessage>(
            Builders<ProcessedMessage>.IndexKeys.Ascending(x => x.MessageId),
            new CreateIndexOptions { Unique = true }));
  }

  public async Task<bool> HasProcessedAsync(string messageId, CancellationToken ct = default)
      => await _collection.Find(x => x.MessageId == messageId).AnyAsync(ct);

  public async Task MarkProcessedAsync(string messageId, CancellationToken ct = default)
  {
    var processed = new ProcessedMessage
    {
      MessageId = messageId,
      ProcessedOn = DateTime.UtcNow
    };

    await _collection.InsertOneAsync(processed, cancellationToken: ct);
  }
}

public class ProcessedMessage
{
  public string MessageId { get; set; } = default!;
  public DateTime ProcessedOn { get; set; }
}
