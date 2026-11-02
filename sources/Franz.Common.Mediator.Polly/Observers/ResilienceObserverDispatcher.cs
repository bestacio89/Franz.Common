using Microsoft.Extensions.Logging;

namespace Franz.Common.Mediator.Polly.Observers;

/// <summary>
/// Dispatches resilience notifications without allowing telemetry failures to alter policy outcomes.
/// </summary>
internal static class ResilienceObserverDispatcher
{
  public static void Notify(
      IEnumerable<IResilienceObserver> observers,
      ILogger logger,
      string policyName,
      Context.ResilienceContext context)
  {
    ArgumentNullException.ThrowIfNull(observers);
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentNullException.ThrowIfNull(context);

    foreach (var observer in observers)
    {
      try
      {
        observer.OnPolicyExecuted(policyName, context);
      }
      catch (Exception exception)
      {
        logger.LogWarning(
            exception,
            "Resilience observer {ObserverType} failed for policy {PolicyName}.",
            observer.GetType().FullName,
            policyName);
      }
    }
  }
}
