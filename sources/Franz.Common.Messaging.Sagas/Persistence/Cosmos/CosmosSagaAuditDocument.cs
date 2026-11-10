#nullable enable

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Cosmos audit document stored in the same tenant partition and container as saga state.
/// It intentionally does not inherit Entity<Guid>: retention must physically delete audit data,
/// not trigger the framework's normal soft-delete behavior.
/// </summary>
internal sealed class CosmosSagaAuditDocument
{
  public Guid Id { get; set; }
  public string TenantId { get; set; } = string.Empty;
  public Guid SagaId { get; set; }
  public string SagaType { get; set; } = string.Empty;
  public string StateType { get; set; } = string.Empty;
  public string StepType { get; set; } = string.Empty;
  public string IncomingMessageType { get; set; } = string.Empty;
  public string? OutgoingMessageType { get; set; }
  public Guid CorrelationId { get; set; }
  public Guid? CausationId { get; set; }
  public Guid? MessageId { get; set; }
  public int Status { get; set; }
  public DateTimeOffset Timestamp { get; set; }
  public long DurationTicks { get; set; }
  public string? ErrorType { get; set; }
}
