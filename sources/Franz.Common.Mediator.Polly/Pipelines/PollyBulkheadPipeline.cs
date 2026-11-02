#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
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

namespace Franz.Common.Mediator.Polly.Pipelines;

/// <summary>
/// Hardened Bulkhead Isolation pipeline.
/// Uses native Guid v7 correlation to track resource pressure and rejections.
/// </summary>
public sealed class PollyBulkheadPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly object _policy;
  private readonly string _policyName;
  private readonly ILogger<TRequest> _logger;
  private readonly IEnumerable<IResilienceObserver> _observers;

  public PollyBulkheadPipeline(
      IReadOnlyPolicyRegistry<string> registry,
      IOptions<PollyBulkheadPipelineOptions> options,
      ILogger<TRequest> logger,
      IEnumerable<IResilienceObserver> observers)
  {
    _policyName = options.Value.PolicyName;
    _policy = registry.Get<object>(_policyName);
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
    var correlationId = MediatorContext.EnsureCorrelationId();

    var stopwatch = Stopwatch.StartNew();
    var context = new ResilienceContext
    {
      PolicyName = _policyName
    };

    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(PollyBulkheadPipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzPolicy", _policyName))
    {
      _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] with Bulkhead {Policy}",
          requestName, correlationId, _policyName);

      try
      {
        TResponse result;

        if (_policy is IAsyncPolicy<TResponse> typedPolicy)
        {
          result = await typedPolicy.ExecuteAsync(async ct => await next(), cancellationToken);
        }
        else if (_policy is IAsyncPolicy untypedPolicy)
        {
          // Capture the result inside the policy; never invoke the downstream chain outside it.
          result = default!;
          await untypedPolicy.ExecuteAsync(async ct =>
          {
            result = await next().ConfigureAwait(false);
          }, cancellationToken);
        }
        else
        {
          throw new InvalidOperationException(
              $"Policy '{_policyName}' does not support asynchronous execution.");
        }

        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;

        if (_policy is AsyncBulkheadPolicy bulkheadPolicy)
        {
          context.BulkheadRejected = false;
          _logger.LogInformation(
              "✅ {Request} [{CorrelationId}] completed via Bulkhead in {Elapsed}ms (policy {Policy}) — Available={Available}, Queue={Queue}",
              requestName, correlationId, stopwatch.ElapsedMilliseconds,
              _policyName, bulkheadPolicy.BulkheadAvailableCount, bulkheadPolicy.QueueAvailableCount);
        }

        NotifyObservers(context);
        return result;
      }
      catch (BulkheadRejectedException bhex)
      {
        HandleException(bhex, context, stopwatch, requestName, correlationId, isRejection: true);
        throw;
      }
      catch (Exception ex)
      {
        HandleException(ex, context, stopwatch, requestName, correlationId, isRejection: false);
        throw;
      }
    }
  }

  private void HandleException(Exception ex, ResilienceContext context, Stopwatch sw, string reqName, Guid corrId, bool isRejection)
  {
    sw.Stop();
    context.Duration = sw.Elapsed;
    context.BulkheadRejected = isRejection;

    if (isRejection)
    {
      _logger.LogWarning(
          ex, "⚠️ {Request} [{CorrelationId}] rejected by Bulkhead {Policy} after {Elapsed}ms",
          reqName, corrId, _policyName, sw.ElapsedMilliseconds);
    }
    else
    {
      _logger.LogError(
          ex, "❌ {Request} [{CorrelationId}] faulted under Bulkhead {Policy} after {Elapsed}ms",
          reqName, corrId, _policyName, sw.ElapsedMilliseconds);
    }

    NotifyObservers(context);
  }

  private void NotifyObservers(ResilienceContext context)
  {
    ResilienceObserverDispatcher.Notify(_observers, _logger, context.PolicyName, context);
  }
}