#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

internal sealed class MongoSagaStoredMessage
{
  [BsonId]
  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid Id { get; set; }
  public string Body { get; set; } = string.Empty;
  public IDictionary<string, string[]> Headers { get; set; }
      = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
  public IDictionary<string, object> Properties { get; set; }
      = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid? CorrelationId { get; set; }
  public string? MessageType { get; set; }
  public DateTime CreatedOn { get; set; }
  public DateTime? SentOn { get; set; }
  public int RetryCount { get; set; }
  public string? LastError { get; set; }
  public DateTime? LastTriedOn { get; set; }
  public DateTime? NextAttemptAt { get; set; }
  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid? LeaseOwnerId { get; set; }
  public DateTime? LeaseExpiresAt { get; set; }
  public bool IsDeadLetter { get; set; }

  public static MongoSagaStoredMessage From(StoredMessage message)
      => new()
      {
        Id = message.Id,
        Body = message.Body,
        Headers = message.Headers,
        Properties = message.Properties,
        CorrelationId = message.CorrelationId,
        MessageType = message.MessageType,
        CreatedOn = message.CreatedOn,
        SentOn = message.SentOn,
        RetryCount = message.RetryCount,
        LastError = message.LastError,
        LastTriedOn = message.LastTriedOn,
        NextAttemptAt = message.NextAttemptAt,
        LeaseOwnerId = message.LeaseOwnerId,
        LeaseExpiresAt = message.LeaseExpiresAt,
        IsDeadLetter = message.IsDeadLetter
      };

  public StoredMessage ToStoredMessage()
      => new()
      {
        Id = Id,
        Body = Body,
        Headers = Headers,
        Properties = Properties,
        CorrelationId = CorrelationId,
        MessageType = MessageType,
        CreatedOn = CreatedOn,
        SentOn = SentOn,
        RetryCount = RetryCount,
        LastError = LastError,
        LastTriedOn = LastTriedOn,
        NextAttemptAt = NextAttemptAt,
        LeaseOwnerId = LeaseOwnerId,
        LeaseExpiresAt = LeaseExpiresAt,
        IsDeadLetter = IsDeadLetter
      };
}
