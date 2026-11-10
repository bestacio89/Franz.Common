#nullable enable

using Franz.Common.Messaging.Sagas.Logging;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// Relational persistence model for metadata-only saga audit entries.
/// </summary>
internal sealed class SagaAuditEntity
{
  public Guid AuditId { get; set; }
  public Guid SagaId { get; set; }
  public string SagaType { get; set; } = string.Empty;
  public string StateType { get; set; } = string.Empty;
  public string StepType { get; set; } = string.Empty;
  public string IncomingMessageType { get; set; } = string.Empty;
  public string? OutgoingMessageType { get; set; }
  public Guid CorrelationId { get; set; }
  public Guid? CausationId { get; set; }
  public Guid? MessageId { get; set; }
  public string? TenantId { get; set; }
  public int Status { get; set; }
  public DateTimeOffset Timestamp { get; set; }
  public long DurationTicks { get; set; }
  public string? ErrorType { get; set; }

  public static SagaAuditEntity From(SagaAuditRecord record)
  {
    ArgumentNullException.ThrowIfNull(record);
    return new SagaAuditEntity
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
      Timestamp = record.Timestamp.ToUniversalTime(),
      DurationTicks = record.Duration.Ticks,
      ErrorType = record.ErrorType
    };
  }
}
