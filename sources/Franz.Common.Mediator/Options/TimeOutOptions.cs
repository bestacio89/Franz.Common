namespace Franz.Common.Mediator.Options
{
  public class TimeoutOptions
  {
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public bool VerboseLogging { get; set; } = false; // log more details in Dev
    public bool Disabled { get; set; } = false;       // e.g. disable in Integration Tests
  }

}
