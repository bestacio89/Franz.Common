namespace Franz.Common.Mediator.Options
{
  public class CircuitBreakerOptions
  {
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    public bool VerboseLogging { get; set; } = false; // detailed info in dev
    public bool Disabled { get; set; } = false;       // disable breaker in tests
  }
}
