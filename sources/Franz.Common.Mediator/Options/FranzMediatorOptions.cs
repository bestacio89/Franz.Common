namespace Franz.Common.Mediator.Options
{
  public class FranzMediatorOptions
  {
    public RetryOptions Retry { get; set; } = new RetryOptions();
    public TimeoutOptions Timeout { get; set; } = new TimeoutOptions();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions();
    public BulkheadOptions Bulkhead { get; set; } = new BulkheadOptions();
    public CachingOptions Caching { get; set; } = new CachingOptions();
    public TransactionOptions Transaction { get; set; } = new TransactionOptions();

    /// <summary>
    /// Enable the built-in console observer by default (optional).
    /// </summary>
    public ConsoleObserverOptions ConsoleObserver { get; set; } = new();
    public bool EnableDefaultConsoleObserver { get; set; } = false;
  }
}
