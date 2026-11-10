#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Logging;

/// <summary>
/// Metadata-only audit entry for a saga transition. Application state and exception messages
/// are deliberately excluded to avoid leaking business data or secrets into audit sinks.
/// </summary>
public sealed record SagaAuditRecord
{
  public Guid AuditId { get; init; } = Guid.CreateVersion7();
  public required Guid SagaId { get; init; }
  public required string SagaType { get; init; }
  public required string StateType { get; init; }
  public required string StepType { get; init; }
  public required string IncomingMessageType { get; init; }
  public string? OutgoingMessageType { get; init; }
  public Guid CorrelationId { get; init; }
  public Guid? CausationId { get; init; }
  public Guid? MessageId { get; init; }
  public string? TenantId { get; init; }
  public SagaLifecycleStatus Status { get; init; }
  public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
  public TimeSpan Duration { get; init; }
  public string? ErrorType { get; init; }
}
