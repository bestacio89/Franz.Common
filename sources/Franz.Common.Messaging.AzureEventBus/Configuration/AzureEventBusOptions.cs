using Azure.Messaging.ServiceBus;

namespace Franz.Common.Messaging.AzureEventBus.Configuration;

public sealed class AzureEventBusOptions
{
  /// <summary>Azure Service Bus connection string.</summary>
  public string ConnectionString { get; set; } = string.Empty;
  public string EntityName { get; set; } = string.Empty;
  /// <summary>
  /// Optional namespace label used for naming strategies / logs (not required by SDK).
  /// </summary>
  public string? Namespace { get; set; }

  /// <summary>
  /// When true, consumers will use session-enabled processors (requires entities configured with sessions).
  /// </summary>
  public bool EnableSessions { get; set; } = false;
  /// <summary>
  /// How long a session can remain idle before the processor closes it.
  /// Only applies when EnableSessions = true.
  /// </summary>
  public TimeSpan SessionIdleTimeout { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  /// Default topic for publishing when none is resolved by a naming strategy (optional).
  /// </summary>
  public string? DefaultTopic { get; set; }

  public AzureEventBusRetryOptions Retry { get; set; } = new();
  public AzureEventBusDeadLetterOptions DeadLetter { get; set; } = new();

  /// <summary>Consumer concurrency for ServiceBusProcessor.</summary>
  public int MaxConcurrentCalls { get; set; } = 8;

  /// <summary>Prefetch for ServiceBusProcessor.</summary>
  public int PrefetchCount { get; set; } = 0;

  /// <summary>How long to auto-renew message locks while processing.</summary>
  public TimeSpan MaxAutoLockRenewalDuration { get; set; } = TimeSpan.FromMinutes(5);

  internal void Validate()
  {
    if (string.IsNullOrWhiteSpace(ConnectionString))
      throw new ArgumentException("AzureEventBusOptions.ConnectionString is required.");

    if (MaxConcurrentCalls <= 0)
      throw new ArgumentOutOfRangeException(nameof(MaxConcurrentCalls), "Must be > 0.");

    if (PrefetchCount < 0)
      throw new ArgumentOutOfRangeException(nameof(PrefetchCount), "Must be >= 0.");

    if (MaxAutoLockRenewalDuration <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(MaxAutoLockRenewalDuration), "Must be > 0.");
  }
  internal ServiceBusProcessorOptions ToProcessorOptions()
  {
    return new ServiceBusProcessorOptions
    {
      AutoCompleteMessages = false,
      MaxConcurrentCalls = MaxConcurrentCalls,
      PrefetchCount = PrefetchCount,
      MaxAutoLockRenewalDuration = MaxAutoLockRenewalDuration
    };
 }
}

