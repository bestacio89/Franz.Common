#nullable enable
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Observers;
using Franz.Common.Mediator.Polly.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Serilog.Context;
using System.Diagnostics;
using PollyContext = Polly.Context;

namespace Franz.Common.Mediator.Polly.Pipelines;

/// <summary>
/// Hardened Retry pipeline.
/// Uses native Guid v7 correlation to group multiple attempts into a single traceable story.
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

    // BAZOOKA REFACTOR: Bridge to the native Guid v7 identity.
    var correlationId = CorrelationId.Ensure();
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

      // Pass the Guid-based identity into Polly's internal context
      var pollyContext = new PollyContext($"Franz-{correlationId}");

      try
      {
        TResponse result;

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
              _logger.LogWarning(ex,
                  "🔁 {Request} [{CorrelationId}] retry attempt {RetryCount} (policy {Policy})",
                  requestName, correlationId, resilienceContext.RetryCount, _policy.PolicyKey);
              throw;
            }
          }, pollyContext, cancellationToken);
        }
        else
        {
          // Untyped execution
          await _policy.ExecuteAsync(async (ctx, ct) =>
          {
            try
            {
              await next();
            }
            catch (Exception ex)
            {
              resilienceContext.RetryCount++;
              _logger.LogWarning(ex,
                  "🔁 {Request} [{CorrelationId}] retry attempt {RetryCount} (policy {Policy})",
                  requestName, correlationId, resilienceContext.RetryCount, _policy.PolicyKey);
              throw;
            }
          }, pollyContext, cancellationToken);

          // Note: Ensure the handler is called one final time to capture the typed result 
          // only if the policy didn't throw.
          result = await next();
        }

        stopwatch.Stop();
        resilienceContext.Duration = stopwatch.Elapsed;

        _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] succeeded after {Elapsed}ms (policy {Policy}, retries={RetryCount})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey, resilienceContext.RetryCount);

        NotifyObservers(resilienceContext);
        return result;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        resilienceContext.Duration = stopwatch.Elapsed;

        _logger.LogError(ex,
            "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms (policy {Policy}, retries={RetryCount})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey, resilienceContext.RetryCount);

        NotifyObservers(resilienceContext);
        throw;
      }
    }
  }

  private void NotifyObservers(Context.ResilienceContext context)
  {
    foreach (var obs in _observers)
      obs.OnPolicyExecuted(context.PolicyName, context);
  }
}