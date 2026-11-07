#nullable enable

using Franz.Common.Business.Domain;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Cosmos document containing saga state and its durable lifecycle metadata.
/// </summary>
public sealed class CosmosSagaStateDocument : Entity<Guid>
{
  public CosmosSagaStateDocument() { }

  public CosmosSagaStateDocument(Guid sagaId) : base(sagaId) { }

  public string TenantId { get; set; } = string.Empty;
  public string SagaType { get; set; } = default!;
  public string StateType { get; set; } = default!;
  public string Payload { get; set; } = default!;
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
