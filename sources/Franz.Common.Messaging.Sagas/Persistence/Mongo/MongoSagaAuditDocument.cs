#nullable enable

using Franz.Common.Messaging.Sagas.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

/// <summary>
/// MongoDB document for a metadata-only saga audit entry.
/// </summary>
internal sealed class MongoSagaAuditDocument
{
  [BsonId]
  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid AuditId { get; set; }

  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid SagaId { get; set; }

  public string SagaType { get; set; } = string.Empty;
  public string StateType { get; set; } = string.Empty;
  public string StepType { get; set; } = string.Empty;
  public string IncomingMessageType { get; set; } = string.Empty;
  public string? OutgoingMessageType { get; set; }

  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid CorrelationId { get; set; }

  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid? CausationId { get; set; }

  [BsonGuidRepresentation(GuidRepresentation.Standard)]
  public Guid? MessageId { get; set; }

  public string? TenantId { get; set; }
  public int Status { get; set; }
  public DateTime TimestampUtc { get; set; }
  public long DurationTicks { get; set; }
  public string? ErrorType { get; set; }

  public static MongoSagaAuditDocument From(SagaAuditRecord record)
  {
    ArgumentNullException.ThrowIfNull(record);
    return new MongoSagaAuditDocument
    {
      AuditId = record.AuditId,
      SagaId = record.SagaId,
      SagaType = record.SagaType,
      StateType = record.StateType,
      StepType = record.StepType,
      IncomingMessageType = record.IncomingMessageType,
      OutgoingMessageType = record.OutgoingMessageType,
      CorrelationId = record.CorrelationId,
      CausationId = record.CausationId,
      MessageId = record.MessageId,
      TenantId = record.TenantId,
      Status = (int)record.Status,
      TimestampUtc = record.Timestamp.UtcDateTime,
      DurationTicks = record.Duration.Ticks,
      ErrorType = record.ErrorType
    };
  }
}
