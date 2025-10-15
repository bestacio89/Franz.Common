using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Context;
using Franz.Common.Mediator.Polly.Observers;
using Franz.Common.Mediator.Polly.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Serilog.Context;
using System.Diagnostics;
using PollyContext = Polly.Context;

namespace Franz.Common.Mediator.Polly.Pipelines
{
  /// <summary>
  /// Retry pipeline with full resilience awareness.
  /// Logs retry attempts, tracks retry count, and notifies observers.
  /// Works with both typed and untyped IAsyncPolicy registrations.
  /// </summary>
  public sealed class PollyRetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<TRequest> _logger;
    private readonly IEnumerable<IResilienceObserver> _observers;

    public PollyRetryPipeline(
        IReadOnlyPolicyRegistry<string> registry,
        IOptions<PollyRetryPipelineOptions> options,
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
      var resilienceContext = new Context.ResilienceContext
      {
        PolicyName = _policy.PolicyKey,
        RetryCount = 0
      };

      using (LogContext.PushProperty("FranzRequest", requestName))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      using (LogContext.PushProperty("FranzPipeline", nameof(PollyRetryPipeline<TRequest, TResponse>)))
      using (LogContext.PushProperty("FranzPolicy", _policy.PolicyKey))
      {
        _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] with Retry policy {Policy}",
          requestName, correlationId, _policy.PolicyKey);

        var pollyContext = new PollyContext($"Franz-{correlationId}");

        try
        {
          TResponse result;

          // ✅ Dual-mode execution (typed/untyped)
          if (_policy is IAsyncPolicy<TResponse> typedPolicy)
          {
            result = await typedPolicy.ExecuteAsync(async (ctx, ct) =>
            {
              try
              {
                return await next();
              }
              catch (Exception ex)
              {
                resilienceContext.RetryCount++;
                _logger.LogWarning(
                  ex,
                  "🔁 {Request} [{CorrelationId}] retry attempt {RetryCount} (policy {Policy})",
                  requestName, correlationId, resilienceContext.RetryCount, _policy.PolicyKey);
                throw;
              }
            },
            pollyContext,
            cancellationToken);
          }
          else
          {
            await _policy.ExecuteAsync(async (ctx, ct) =>
            {
              try
              {
                await next();
              }
              catch (Exception ex)
              {
                resilienceContext.RetryCount++;
                _logger.LogWarning(
                  ex,
                  "🔁 {Request} [{CorrelationId}] retry attempt {RetryCount} (policy {Policy})",
                  requestName, correlationId, resilienceContext.RetryCount, _policy.PolicyKey);
                throw;
              }
            },
            pollyContext,
            cancellationToken);

            // Execute once more to produce the typed result
            result = await next();
          }

          stopwatch.Stop();
          resilienceContext.Duration = stopwatch.Elapsed;

          _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] succeeded after {Elapsed}ms (policy {Policy}, retries={RetryCount})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey, resilienceContext.RetryCount);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(resilienceContext.PolicyName, resilienceContext);

          return result;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          resilienceContext.Duration = stopwatch.Elapsed;

          _logger.LogError(
            ex,
            "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms (policy {Policy}, retries={RetryCount})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey, resilienceContext.RetryCount);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(resilienceContext.PolicyName, resilienceContext);

          throw;
        }
      }
    }
  }
}
