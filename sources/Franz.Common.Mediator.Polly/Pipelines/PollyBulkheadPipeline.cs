using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Context;
using Franz.Common.Mediator.Polly.Observers;
using Franz.Common.Mediator.Polly.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Bulkhead;
using Polly.Registry;
using Serilog.Context;
using System.Diagnostics;
using ResilienceContext = Franz.Common.Mediator.Polly.Context.ResilienceContext;

namespace Franz.Common.Mediator.Polly.Pipelines
{
  /// <summary>
  /// Bulkhead Isolation pipeline with full resilience awareness.
  /// Tracks rejected tasks, queue capacity, and execution duration.
  /// Works with both typed and untyped IAsyncPolicy registrations.
  /// </summary>
  public sealed class PollyBulkheadPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<TRequest> _logger;
    private readonly IEnumerable<IResilienceObserver> _observers;

    public PollyBulkheadPipeline(
        IReadOnlyPolicyRegistry<string> registry,
        IOptions<PollyBulkheadPipelineOptions> options,
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
      using (LogContext.PushProperty("FranzPipeline", nameof(PollyBulkheadPipeline<TRequest, TResponse>)))
      using (LogContext.PushProperty("FranzPolicy", _policy.PolicyKey))
      {
        _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] with Bulkhead {Policy}",
          requestName, correlationId, _policy.PolicyKey);

        try
        {
          TResponse result;

          // ✅ Handle both typed and untyped policy cases
          if (_policy is IAsyncPolicy<TResponse> typedPolicy)
          {
            result = await typedPolicy.ExecuteAsync(async ct => await next(), cancellationToken);
          }
          else
          {
            // Untyped execution fallback (generic-safe)
            await _policy.ExecuteAsync(async ct => await next(), cancellationToken);
            result = await next(); // execute delegate to get typed result
          }

          stopwatch.Stop();
          context.Duration = stopwatch.Elapsed;

          if (_policy is AsyncBulkheadPolicy bulkheadPolicy)
          {
            context.BulkheadRejected = false;
            _logger.LogInformation(
              "✅ {Request} [{CorrelationId}] completed via Bulkhead in {Elapsed}ms (policy {Policy}) — Available={Available}, Queue={Queue}",
              requestName, correlationId, stopwatch.ElapsedMilliseconds,
              _policy.PolicyKey, bulkheadPolicy.BulkheadAvailableCount, bulkheadPolicy.QueueAvailableCount);
          }

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(context.PolicyName, context);

          return result;
        }
        catch (BulkheadRejectedException bhex)
        {
          stopwatch.Stop();
          context.Duration = stopwatch.Elapsed;
          context.BulkheadRejected = true;

          _logger.LogWarning(
            bhex,
            "⚠️ {Request} [{CorrelationId}] rejected by Bulkhead {Policy} (capacity reached) after {Elapsed}ms",
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
            "❌ {Request} [{CorrelationId}] faulted under Bulkhead {Policy} after {Elapsed}ms",
            requestName, correlationId, _policy.PolicyKey, stopwatch.ElapsedMilliseconds);

          foreach (var obs in _observers)
            obs.OnPolicyExecuted(context.PolicyName, context);

          throw;
        }
      }
    }
  }
}
