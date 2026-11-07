#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Persistence;

/// <summary>
/// Persistence envelope separating orchestration metadata from application-owned saga state.
/// </summary>
public sealed record SagaPersistenceRecord
{
  public required Guid SagaId { get; init; }
  public required string SagaType { get; init; }
  public required object State { get; init; }
  public SagaLifecycleStatus Status { get; init; } = SagaLifecycleStatus.Running;
  public long Version { get; init; }
  public string? TenantId { get; init; }
  public required DateTimeOffset CreatedAt { get; init; }
  public required DateTimeOffset UpdatedAt { get; init; }
  public DateTimeOffset? CompletedAt { get; init; }
  public DateTimeOffset? FailedAt { get; init; }
  public DateTimeOffset? NextAttemptAt { get; init; }
  public Guid? LastMessageId { get; init; }
  public string? LastError { get; init; }
}
