namespace Franz.Common.Mediator.Options
{
  public class TimeoutOptions
  {
    /// <summary>
    /// Maximum duration allowed before timing out a request.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
  }
}
