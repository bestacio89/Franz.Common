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
  /// Maximum number of durable retry schedules allowed for one consecutive saga step.
  /// </summary>
  public int MaxRetryAttempts { get; set; } = 10;

  /// <summary>
  /// Delay used when a retry transition does not specify an explicit delay.
  /// </summary>
  public TimeSpan DefaultRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  /// Retains durable saga audit records for this duration. Null disables automatic cleanup.
  /// </summary>
  public TimeSpan? AuditRetentionPeriod { get; set; }

  /// <summary>
  /// Interval between bounded audit-retention cleanup batches.
  /// </summary>
  public TimeSpan AuditRetentionCleanupInterval { get; set; } = TimeSpan.FromHours(1);

  /// <summary>
  /// Maximum number of expired audit records removed by one cleanup cycle.
  /// </summary>
  public int AuditRetentionBatchSize { get; set; } = 500;

  /// <summary>
  /// Optional EF schema for relational saga persistence.
  /// </summary>
  public string? EntityFrameworkSchema { get; set; }
}
