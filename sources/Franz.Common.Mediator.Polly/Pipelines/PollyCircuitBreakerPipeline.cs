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

namespace Franz.Common.Mediator.Polly.Pipelines
{
  /// <summary>
  /// CircuitBreaker pipeline with full resilience awareness.
  /// Works with both typed and untyped IAsyncPolicy registrations.
  /// Tracks circuit state, execution duration, and emits resilience telemetry.
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
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

      var stopwatch = Stopwatch.StartNew();
      var context = new ResilienceContext
      {
        PolicyName = _policy.PolicyKey
      };

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

          // ✅ Handle both typed and untyped policies gracefully
          if (_policy is IAsyncPolicy<TResponse> typedPolicy)
          {
            result = await typedPolicy.ExecuteAsync(async ct => await next(), cancellationToken);
          }
          else
          {
            await _policy.ExecuteAsync(async ct => await next(), cancellationToken);
            result = await next(); // fallback if untyped
          }

          stopwatch.Stop();
          context.Duration = stopwatch.Elapsed;

          if (_policy is AsyncCircuitBreakerPolicy breaker)
            context.CircuitOpen = breaker.CircuitState == CircuitState.Open;

          _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] passed CircuitBreaker in {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(context.PolicyName, context);

          return result;
        }
        catch (BrokenCircuitException bcex)
        {
          stopwatch.Stop();
          context.Duration = stopwatch.Elapsed;
          context.CircuitOpen = true;

          _logger.LogWarning(
            bcex,
            "⚠️ {Request} [{CorrelationId}] blocked by open circuit ({Policy}) after {Elapsed}ms",
            requestName, correlationId, _policy.PolicyKey, stopwatch.ElapsedMilliseconds);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(context.PolicyName, context);

          throw;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          context.Duration = stopwatch.Elapsed;

          _logger.LogError(
            ex,
            "❌ {Request} [{CorrelationId}] failed within CircuitBreaker after {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(context.PolicyName, context);

          throw;
        }
      }
    }
  }
}
