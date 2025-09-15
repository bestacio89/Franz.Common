namespace Franz.Common.Mediator.Options
{
  public class RetryOptions
  {
    /// <summary>
    /// Maximum number of retry attempts before failing.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between attempts.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(200);
  }
}
