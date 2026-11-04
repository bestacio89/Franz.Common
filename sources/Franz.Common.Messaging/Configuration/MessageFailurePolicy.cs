namespace Franz.Common.Messaging.Configuration;

/// <summary>
/// Defines broker-neutral handling for a message that fails during processing.
/// Transport adapters map this policy to their native retry and dead-letter mechanisms.
/// </summary>
public sealed record MessageFailurePolicy
{
  public MessageFailurePolicy(
      int maxRetryAttempts = 3,
      TimeSpan? retryBackoff = null,
      bool deadLetterOnExhaustion = true)
  {
    if (maxRetryAttempts < 0)
      throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Retry attempts cannot be negative.");

    var effectiveBackoff = retryBackoff ?? TimeSpan.FromMilliseconds(500);
    if (effectiveBackoff < TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(retryBackoff), "Retry backoff cannot be negative.");

    MaxRetryAttempts = maxRetryAttempts;
    RetryBackoff = effectiveBackoff;
    DeadLetterOnExhaustion = deadLetterOnExhaustion;
  }

  /// <summary>Number of retries after the initial delivery.</summary>
  public int MaxRetryAttempts { get; }

  /// <summary>Delay between consecutive attempts.</summary>
  public TimeSpan RetryBackoff { get; }

  /// <summary>Whether exhausted messages should be sent to a dead-letter destination.</summary>
  public bool DeadLetterOnExhaustion { get; }

  public static MessageFailurePolicy Default { get; } = new();
}
