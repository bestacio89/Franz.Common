#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
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
/// Hardened Circuit Breaker pipeline.
/// Protects the system while maintaining the Guid v7 "Golden Thread" for every failure.
/// </summary>
public sealed class PollyCircuitBreakerPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly IAsyncPolicy _policy;
  private readonly ILogger<TRequest> _logger;
  private readonly IEnumerable<IResilienceObserver> _observers;

  public PollyCircuitBreakerPipeline(
      IReadOnlyPolicyRegistry<string> registry,
      IOptions<PollyCircuitBreakerPipelineOptions> options,
      ILogger<TRequest> logger,
      IEnumerable<IResilienceObserver> observers)
  {
    _policy = registry.Get<IAsyncPolicy>(options.Value.PolicyName);
    _logger = logger;
    _observers = observers;
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    var requestName = request?.GetType().Name ?? typeof(TRequest).Name;

    // BAZOOKA REFACTOR: Fetch or anchor the native Guid v7 identity.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

    var stopwatch = Stopwatch.StartNew();
    var context = new ResilienceContext { PolicyName = _policy.PolicyKey };

    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(PollyCircuitBreakerPipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzPolicy", _policy.PolicyKey))
    {
      _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] with CircuitBreaker {Policy}",
          requestName, correlationId, _policy.PolicyKey);

      try
      {
        TResponse result;

        if (_policy is IAsyncPolicy<TResponse> typedPolicy)
        {
          result = await typedPolicy.ExecuteAsync(async ct => await next(), cancellationToken);
        }
        else
        {
          // Untyped execution. Note: Ensure next() is only called via policy execution.
          await _policy.ExecuteAsync(async ct => { await next(); }, cancellationToken);
          result = await next();
        }

        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;

        if (_policy is AsyncCircuitBreakerPolicy breaker)
          context.CircuitOpen = breaker.CircuitState == CircuitState.Open;

        _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] passed CircuitBreaker in {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

        NotifyObservers(context);
        return result;
      }
      catch (BrokenCircuitException bcex)
      {
        HandleException(bcex, context, stopwatch, requestName, correlationId, isBreaker: true);
        throw;
      }
      catch (Exception ex)
      {
        HandleException(ex, context, stopwatch, requestName, correlationId, isBreaker: false);
        throw;
      }
    }
  }

  private void HandleException(Exception ex, ResilienceContext context, Stopwatch sw, string reqName, Guid corrId, bool isBreaker)
  {
    sw.Stop();
    context.Duration = sw.Elapsed;
    context.CircuitOpen = isBreaker;

    if (isBreaker)
    {
      _logger.LogWarning(
          ex, "⚠️ {Request} [{CorrelationId}] blocked by open circuit ({Policy}) after {Elapsed}ms",
          reqName, corrId, _policy.PolicyKey, sw.ElapsedMilliseconds);
    }
    else
    {
      _logger.LogError(
          ex, "❌ {Request} [{CorrelationId}] failed within CircuitBreaker after {Elapsed}ms (policy {Policy})",
          reqName, corrId, sw.ElapsedMilliseconds, _policy.PolicyKey);
    }

    NotifyObservers(context);
  }

  private void NotifyObservers(ResilienceContext context)
  {
    foreach (var obs in _observers)
      obs.OnPolicyExecuted(context.PolicyName, context);
  }
}