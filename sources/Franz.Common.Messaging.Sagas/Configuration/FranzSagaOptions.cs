#nullable enable

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// Options for configuring the Saga subsystem.
/// Loaded from the "Franz:Sagas" configuration section.
/// </summary>
public sealed class FranzSagaOptions
{
  /// <summary>
  /// Supported lifecycle repositories: Memory, EntityFramework, Mongo, and Cosmos.
  /// Kafka and Redis state repositories are not implemented.
  /// </summary>
  public string Persistence { get; set; } = "Memory";

  /// <summary>
  /// Enables strict validation of saga message mappings during startup.
  /// </summary>
  public bool ValidateMappings { get; set; } = true;

  /// <summary>
  /// Enables saga audit sink writes.
  /// </summary>
  public bool EnableAuditing { get; set; }

  /// <summary>
  /// Optional EF schema for relational saga persistence.
  /// </summary>
  public string? EntityFrameworkSchema { get; set; }
}
