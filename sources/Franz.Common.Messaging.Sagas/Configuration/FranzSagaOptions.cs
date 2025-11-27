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
  /// Alias of ValidateMappings for backward compatibility.
  /// </summary>
  public bool EnableValidation
  {
    get => ValidateMappings;
    set => ValidateMappings = value;
  }

  /// <summary>
  /// Strict mapping validation used by SagaBuilder.Build().
  /// </summary>
  public bool ValidateMappings { get; set; } = true;

  /// <summary>
  /// Whether audit logs should be written.
  /// </summary>
  public bool EnableAuditing { get; set; } = false;

  /// <summary>
  /// Optional: Kafka compacted topic for saga state.
  /// </summary>
  public string? KafkaStateTopic { get; set; }

  /// <summary>
  /// Redis connection string for Redis persistence.
  /// </summary>
  public string? RedisConnectionString { get; set; }

  /// <summary>
  /// Optional EF schema for saga persistence.
  /// </summary>
  public string? EntityFrameworkSchema { get; set; }
}
