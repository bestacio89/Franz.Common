namespace Franz.Common.Mediator.Options
{
  public class BulkheadOptions
  {
    /// <summary>
    /// Maximum number of concurrent requests allowed.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;
  }
}
