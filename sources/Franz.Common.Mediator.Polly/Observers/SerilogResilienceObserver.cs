using Franz.Common.Mediator.Polly.Context;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Mediator.Polly.Observers
{
  /// <summary>
  /// Default observer that logs policy executions via Serilog / ILogger.
  /// </summary>
  public sealed class SerilogResilienceObserver : IResilienceObserver
  {
    private readonly ILogger<SerilogResilienceObserver> _logger;

    public SerilogResilienceObserver(ILogger<SerilogResilienceObserver> logger)
      => _logger = logger;

    public void OnPolicyExecuted(string policyName, ResilienceContext context)
    {
      _logger.LogInformation(
        "🛡️ [Resilience] Policy={Policy} Retries={Retries} CircuitOpen={CircuitOpen} Timeout={Timeout} Bulkhead={Bulkhead} Duration={Duration}ms Healthy={Healthy}",
        policyName,
        context.RetryCount,
        context.CircuitOpen,
        context.TimeoutOccurred,
        context.BulkheadRejected,
        context.Duration.TotalMilliseconds,
        context.IsHealthy);
    }
  }
}
