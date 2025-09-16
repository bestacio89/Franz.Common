namespace Franz.Common.Mediator.Options
{
  public class CircuitBreakerOptions
  {
    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// How long the circuit remains open before allowing attempts again.
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(60);
  }
}
