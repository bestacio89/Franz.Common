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
  private readonly IsPolicy _policy;
  private readonly string _policyName;
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
    _policyName = options.Value.PolicyName;
    _policy = registry.Get<IsPolicy>(_policyName);
  }

  public async Task<TResponse> Handle(
     TRequest request,
     Func<Task<TResponse>> next,
     CancellationToken cancellationToken = default)
  {
    var requestName = request?.GetType().Name ?? typeof(TRequest).Name;
    var correlationId = MediatorContext.EnsureCorrelationId();
    var stopwatch = Stopwatch.StartNew();

    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPolicy", _policyName))
    {
      _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] via {Policy}",
          requestName, correlationId, _policyName);

      var context = new ResilienceContext
      {
        PolicyName = _policyName
      };

      try
      {
        TResponse result;

        if (_policy is IAsyncPolicy<TResponse> typedPolicy)
        {
          result = await typedPolicy.ExecuteAsync(_ => next(), cancellationToken);
        }
        else if (_policy is IAsyncPolicy untypedPolicy)
        {
          result = default!;
          await untypedPolicy.ExecuteAsync(async _ =>
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
    ResilienceObserverDispatcher.Notify(_observers, _logger, context.PolicyName, context);
  }
}