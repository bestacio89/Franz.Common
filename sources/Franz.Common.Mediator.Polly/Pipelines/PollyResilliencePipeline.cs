#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Observers;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Serilog.Context;
using System.Diagnostics;
using ResilienceContext = Franz.Common.Mediator.Polly.Context.ResilienceContext;

namespace Franz.Common.Mediator.Polly.Pipelines;

/// <summary>
/// Hardened composite Polly pipeline using native Guid v7 correlation.
/// Executes Retry → Timeout → CircuitBreaker → Bulkhead in a single execution flow.
/// </summary>
public sealed class PollyResiliencePipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly IReadOnlyPolicyRegistry<string> _registry;
  private readonly ILogger<TRequest> _logger;
  private readonly IEnumerable<IResilienceObserver> _observers;
  private readonly string _profileName;

  public PollyResiliencePipeline(
      IReadOnlyPolicyRegistry<string> registry,
      ILogger<TRequest> logger,
      IEnumerable<IResilienceObserver> observers,
      string profileName = "Default")
  {
    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _observers = observers ?? Array.Empty<IResilienceObserver>();
    _profileName = profileName;
  }

  public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
  {
    var requestName = request?.GetType().Name ?? typeof(TRequest).Name;

    // Ensure we have a native Guid v7 correlation ID
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

    var stopwatch = Stopwatch.StartNew();
    var context = new ResilienceContext { PolicyName = _profileName };

    // Pushing Guid directly to LogContext for optimized storage in sinks (Seq, ELK, SQL)
    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(PollyResiliencePipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzProfile", _profileName))
    {
      _logger.LogInformation("▶️ Executing {Request} [{CorrelationId}] via profile {Profile}", requestName, correlationId, _profileName);

      try
      {
        if (!_registry.TryGet<IAsyncPolicy>($"mediator:{_profileName}", out var compositePolicy))
          throw new InvalidOperationException($"Resilience profile '{_profileName}' is not registered.");

        TResponse result;

        if (compositePolicy is IAsyncPolicy<TResponse> typedPolicy)
        {
          result = await typedPolicy.ExecuteAsync(async _ => await next(), cancellationToken);
        }
        else
        {
          // FIX: Ensure 'next' is executed through the policy to maintain bulkhead/retry integrity
          result = await compositePolicy.ExecuteAsync(async _ => await next(), cancellationToken);
        }

        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;

        if (compositePolicy is AsyncCircuitBreakerPolicy breaker)
          context.CircuitOpen = breaker.CircuitState == CircuitState.Open;

        _logger.LogInformation("✅ {Request} [{CorrelationId}] succeeded in {Elapsed}ms via profile {Profile}",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _profileName);

        NotifyObservers(_profileName, context);

        return result;
      }
      catch (BrokenCircuitException bcex)
      {
        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;
        context.CircuitOpen = true;

        _logger.LogWarning(bcex, "⚠️ {Request} [{CorrelationId}] blocked — circuit OPEN under profile {Profile} after {Elapsed}ms",
            requestName, correlationId, _profileName, stopwatch.ElapsedMilliseconds);

        NotifyObservers(_profileName, context);
        throw;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        context.Duration = stopwatch.Elapsed;

        _logger.LogError(ex, "❌ {Request} [{CorrelationId}] failed in {Elapsed}ms under profile {Profile}",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _profileName);

        NotifyObservers(_profileName, context);
        throw;
      }
    }
  }

  private void NotifyObservers(string profile, ResilienceContext context)
  {
    foreach (var obs in _observers)
      obs.OnPolicyExecuted(profile, context);
  }
}