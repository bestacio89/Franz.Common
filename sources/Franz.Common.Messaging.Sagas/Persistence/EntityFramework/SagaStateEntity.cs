#nullable enable

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// Relational persistence envelope for a saga instance, scoped by tenant and saga identity.
/// </summary>
public sealed class SagaStateEntity
{
  public required Guid SagaId { get; set; }
  public Guid CorrelationId { get; set; }
  public string TenantId { get; set; } = string.Empty;
  public required string SagaType { get; set; }
  public required string StateType { get; set; }
  public required string SerializedState { get; set; }
  public int Status { get; set; }
  public long Version { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }
  public DateTimeOffset? CompletedAt { get; set; }
  public DateTimeOffset? FailedAt { get; set; }
  public DateTimeOffset? NextAttemptAt { get; set; }
  public Guid? LastMessageId { get; set; }
  public string? LastError { get; set; }
}
