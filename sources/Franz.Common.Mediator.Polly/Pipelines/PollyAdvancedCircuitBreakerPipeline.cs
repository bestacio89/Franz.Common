#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Context;
using Franz.Common.Mediator.Polly.Observers;
using Franz.Common.Mediator.Polly.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Serilog.Context;
using System.Diagnostics;
using ResilienceContext = Franz.Common.Mediator.Polly.Context.ResilienceContext;

namespace Franz.Common.Mediator.Polly.Pipelines;

/// <summary>
/// Hardened Advanced Circuit Breaker pipeline.
/// Enforces high-performance native Guid v7 correlation and accurate execution wrapping.
/// </summary>
public sealed class PollyAdvancedCircuitBreakerPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly IAsyncPolicy _policy;
  private readonly ILogger<TRequest> _logger;
  private readonly IEnumerable<IResilienceObserver> _observers;

  public PollyAdvancedCircuitBreakerPipeline(
      IReadOnlyPolicyRegistry<string> registry,
      IOptions<PollyCircuitBreakerPipelineOptions> options,
      ILogger<TRequest> logger,
      IEnumerable<IResilienceObserver> observers)
  {
    _logger = logger;
    _observers = observers;
    _policy = registry.Get<IAsyncPolicy>(options.Value.PolicyName);
  }

  public async Task<TResponse> Handle(
     TRequest request,
     Func<Task<TResponse>> next,
     CancellationToken cancellationToken = default)
  {
    var requestName = request?.GetType().Name ?? typeof(TRequest).Name;
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();
    var stopwatch = Stopwatch.StartNew();

    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPolicy", _policy.PolicyKey))
    {
      _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] via {Policy}",
          requestName, correlationId, _policy.PolicyKey);

      var context = new ResilienceContext
      {
        PolicyName = _policy.PolicyKey
      };

      try
      {
        var result = await _policy.ExecuteAsync(_ => next(), cancellationToken);

        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;

        _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] completed in {Elapsed}ms",
            requestName, correlationId, stopwatch.ElapsedMilliseconds);

        NotifyObservers(context);
        return result;
      }
      catch (BrokenCircuitException ex)
      {
        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;
        context.CircuitOpen = true;

        _logger.LogWarning(
            ex,
            "⚠️ {Request} [{CorrelationId}] blocked by circuit breaker",
            requestName, correlationId);

        NotifyObservers(context);
        throw;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;

        _logger.LogError(
            ex,
            "❌ {Request} [{CorrelationId}] failed",
            requestName, correlationId);

        NotifyObservers(context);
        throw;
      }
    }
  }

  private void NotifyObservers(ResilienceContext context)
  {
    foreach (var obs in _observers)
      obs.OnPolicyExecuted(context.PolicyName, context);
  }
}