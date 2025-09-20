namespace Franz.Common.Mediator.Options
{
  public class BulkheadOptions
  {
    public int MaxConcurrentRequests { get; set; } = 10;
    public int? MaxQueueLength { get; set; } = null; // optional waiting queue
    public bool VerboseLogging { get; set; } = false;
    public bool Disabled { get; set; } = false;
  }
}
