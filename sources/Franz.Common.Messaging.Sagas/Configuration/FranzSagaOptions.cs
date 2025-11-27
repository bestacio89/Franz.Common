#nullable enable

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// Options for configuring the Saga subsystem.
/// Loaded automatically from appsettings.json: section "Franz:Sagas"
/// </summary>
public sealed class FranzSagaOptions
{
  /// <summary>
  /// Allowed: "Memory", "EntityFramework", "Redis", "Kafka"
  /// </summary>
  public string Persistence { get; set; } = "Memory";

  /// <summary>
  /// Whether saga mappings should be validated during application startup.
  /// </summary>
  public bool EnableValidation { get; set; } = true;

  /// <summary>
  /// Whether audit logs should be written.
  /// </summary>
  public bool EnableAuditing { get; set; } = false;

  /// <summary>
  /// Optional: name of the Kafka compacted topic to use for state storage.
  /// Only applies if Persistence == "Kafka"
  /// </summary>
  public string? KafkaStateTopic { get; set; }

  /// <summary>
  /// Optional: Redis connection string if Persistence == "Redis".
  /// </summary>
  public string? RedisConnectionString { get; set; }

  /// <summary>
  /// Optional schema for the EF-based saga persistence.
  /// </summary>
  public string? EntityFrameworkSchema { get; set; }
}
