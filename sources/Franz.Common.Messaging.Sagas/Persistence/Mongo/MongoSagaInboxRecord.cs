#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

internal sealed class MongoSagaInboxRecord
{
  [BsonId]
  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid MessageId { get; set; }
  public DateTime ProcessedOn { get; set; }
}
