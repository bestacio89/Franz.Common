using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Context;
using Franz.Common.Mediator.Polly.Observers;
using Franz.Common.Mediator.Polly.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Timeout;
using Serilog.Context;
using System.Diagnostics;
using PollyContext = Polly.Context;
using ResilienceContext = Franz.Common.Mediator.Polly.Context.ResilienceContext;

namespace Franz.Common.Mediator.Polly.Pipelines
{
  /// <summary>
  /// Timeout pipeline — enforces execution time limits and reports timeout metrics.
  /// Works with both typed and untyped IAsyncPolicy registrations.
  /// Integrates full Franz resilience awareness (correlation, observers, duration, health).
  /// </summary>
  public sealed class PollyTimeoutPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<TRequest> _logger;
    private readonly IEnumerable<IResilienceObserver> _observers;

    public PollyTimeoutPipeline(
        IReadOnlyPolicyRegistry<string> registry,
        IOptions<PollyTimeoutPipelineOptions> options,
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
      var resilienceContext = new ResilienceContext
      {
        PolicyName = _policy.PolicyKey
      };

      using (LogContext.PushProperty("FranzRequest", requestName))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      using (LogContext.PushProperty("FranzPipeline", nameof(PollyTimeoutPipeline<TRequest, TResponse>)))
      using (LogContext.PushProperty("FranzPolicy", _policy.PolicyKey))
      {
        _logger.LogInformation(
          "⏳ Executing {Request} [{CorrelationId}] with Timeout policy {Policy}",
          requestName, correlationId, _policy.PolicyKey);

        var pollyContext = new PollyContext($"Franz-{correlationId}");

        try
        {
          TResponse result;

          // ✅ Supports both typed and untyped timeout policies
          if (_policy is IAsyncPolicy<TResponse> typedPolicy)
          {
            result = await typedPolicy.ExecuteAsync(async (ctx, ct) => await next(), pollyContext, cancellationToken);
          }
          else
          {
            await _policy.ExecuteAsync(async (ctx, ct) => await next(), pollyContext, cancellationToken);
            result = await next(); // fallback to produce typed result
          }

          stopwatch.Stop();
          resilienceContext.Duration = stopwatch.Elapsed;

          _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] completed within timeout in {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(resilienceContext.PolicyName, resilienceContext);

          return result;
        }
        catch (TimeoutRejectedException ex)
        {
          stopwatch.Stop();
          resilienceContext.Duration = stopwatch.Elapsed;
          resilienceContext.TimeoutOccurred = true;

          _logger.LogError(
            ex,
            "⏱️ {Request} [{CorrelationId}] timed out after {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(resilienceContext.PolicyName, resilienceContext);

          throw;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          resilienceContext.Duration = stopwatch.Elapsed;

          _logger.LogError(
            ex,
            "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(resilienceContext.PolicyName, resilienceContext);

          throw;
        }
      }
    }
  }
}
