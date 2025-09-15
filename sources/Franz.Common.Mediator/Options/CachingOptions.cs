namespace Franz.Common.Mediator.Options
{
  public class CachingOptions
  {
    /// <summary>
    /// Default time-to-live for cached items.
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of items to cache.
    /// </summary>
    public int MaxItems { get; set; } = 1000;
  }
}
