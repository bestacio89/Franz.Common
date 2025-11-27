#nullable enable

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// EF entity storing serialized saga state.
/// </summary>
public sealed class SagaStateEntity
{
  public required string SagaId { get; set; }
  public required string SagaType { get; set; }
  public required string SerializedState { get; set; }
}
