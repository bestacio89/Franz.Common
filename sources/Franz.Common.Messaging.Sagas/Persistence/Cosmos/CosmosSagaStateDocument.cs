#nullable enable

using Franz.Common.Business.Domain;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Cosmos document for saga state, inheriting Franz Entity conventions.
/// </summary>
public sealed class CosmosSagaStateDocument : Entity<string>
{
  public CosmosSagaStateDocument(string sagaId)
  {
    Id = sagaId;
  }
  /// <summary>
  /// Fully-qualified .NET type name of the saga state (for polymorphic deserialization).
  /// </summary>
  public string SagaType { get; set; } = default!;

  /// <summary>
  /// Raw JSON serialized saga state.
  /// </summary>
  public string Payload { get; set; } = default!;

  /// <summary>
  /// Optional concurrency token (used by ISagaStateWithId).
  /// </summary>
  public string? ConcurrencyToken { get; set; }

  /// <summary>
  /// Server timestamp for change tracking (Cosmos-friendly).
  /// </summary>
  public DateTime UpdatedAt { get; set; }
}
