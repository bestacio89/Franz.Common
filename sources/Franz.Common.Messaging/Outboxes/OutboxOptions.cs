namespace Franz.Common.Messaging.Outbox;

public class OutboxOptions
{
  /// <summary>
  /// Enables or disables the outbox publisher.
  /// Default = false.
  /// </summary>
  public bool Enabled { get; set; } = false;

  /// <summary>
  /// Polling interval for checking pending messages.
  /// Default = 5 seconds.
  /// </summary>
  public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

  /// <summary>
  /// Maximum number of retries before moving a message to dead-letter.
  /// Default = 3.
  /// </summary>
  public int MaxRetries { get; set; } = 3;

  /// <summary>
  /// Enable dead-letter queue for failed messages.
  /// Default = false.
  /// </summary>
  public bool DeadLetterEnabled { get; set; } = false;
}
