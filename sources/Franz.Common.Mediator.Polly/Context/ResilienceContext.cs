namespace Franz.Common.Mediator.Polly.Context
{
  /// <summary>
  /// Carries runtime resilience metrics across pipeline execution.
  /// Tracks retries, circuit state, timeouts, and bulkhead pressure.
  /// </summary>
  public sealed class ResilienceContext
  {
    public string PolicyName { get; init; } = string.Empty;
    public int RetryCount { get; set; } = 0;
    public bool CircuitOpen { get; set; } = false;
    public bool TimeoutOccurred { get; set; } = false;
    public bool BulkheadRejected { get; set; } = false;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    public bool IsHealthy => !CircuitOpen && !TimeoutOccurred && !BulkheadRejected;
  }
}
