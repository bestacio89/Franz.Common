using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Storage;
using global::MongoDB.Driver;


namespace Franz.Common.MongoDB.Repositories;
public class MongoMessageStore : IMessageStore
{
  private readonly IMongoCollection<StoredMessage> _collection;

  public MongoMessageStore(IMongoDatabase database, string collectionName = "OutboxMessages")
  {
    _collection = database.GetCollection<StoredMessage>(collectionName);
  }

  public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
  {
    var stored = new StoredMessage
    {
      Body = message.Body,
      Headers = message.Headers.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Where(v => v != null).ToArray() // StringValues -> string[]
        ),
      Properties = new Dictionary<string, object>(message.Properties)
    };

    await _collection.InsertOneAsync(stored, cancellationToken: cancellationToken);
  }


  public async Task<IReadOnlyList<Message>> GetPendingAsync(CancellationToken cancellationToken = default)
  {
    var pending = await _collection
        .Find(m => m.SentOn == null)
        .ToListAsync(cancellationToken);

    return pending.Select(p => new Message(
        p.Body,
        p.Headers.Select(h => new KeyValuePair<string, string[]>(h.Key, h.Value))
    )
    {
      Properties = p.Properties
    }).ToList();
  }

  public async Task MarkAsSentAsync(string messageId, CancellationToken cancellationToken = default)
  {
    var update = Builders<StoredMessage>.Update.Set(m => m.SentOn, DateTime.UtcNow);
    await _collection.UpdateOneAsync(
        m => m.Id == messageId,
        update,
        cancellationToken: cancellationToken
    );
  }
}
