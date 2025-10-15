using Franz.Common.Mediator.Polly.Context;

namespace Franz.Common.Mediator.Polly.Observers
{
  /// <summary>
  /// Observes the execution lifecycle of resilience policies for telemetry or logging.
  /// </summary>
  public interface IResilienceObserver
  {
    void OnPolicyExecuted(string policyName, ResilienceContext context);
  }
}
