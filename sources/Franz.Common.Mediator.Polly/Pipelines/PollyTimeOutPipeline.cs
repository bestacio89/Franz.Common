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
using Polly.Registry;
using Polly.Timeout;
using Serilog.Context;
using System.Diagnostics;
using PollyContext = Polly.Context;
using ResilienceContext = Franz.Common.Mediator.Polly.Context.ResilienceContext;

namespace Franz.Common.Mediator.Polly.Pipelines;

/// <summary>
/// Hardened Timeout pipeline — enforces execution time limits with native Guid v7 correlation.
/// </summary>
public sealed class PollyTimeoutPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly IsPolicy _policy;
  private readonly string _policyName;
  private readonly ILogger<TRequest> _logger;
  private readonly IEnumerable<IResilienceObserver> _observers;

  public PollyTimeoutPipeline(
      IReadOnlyPolicyRegistry<string> registry,
      IOptions<PollyTimeoutPipelineOptions> options,
      ILogger<TRequest> logger,
      IEnumerable<IResilienceObserver> observers)
  {
    _policyName = options.Value.PolicyName;
    _policy = registry.Get<IsPolicy>(_policyName);
    _logger = logger;
    _observers = observers;
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    var requestName = request?.GetType().Name ?? typeof(TRequest).Name;
    cancellationToken.ThrowIfCancellationRequested();

    // Ensure we have a native Guid v7 correlation ID
    var correlationId = MediatorContext.EnsureCorrelationId(); 

    var stopwatch = Stopwatch.StartNew();
    var resilienceContext = new ResilienceContext
    {
      PolicyName = _policyName
    };

    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(PollyTimeoutPipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzPolicy", _policyName))
    {
      _logger.LogInformation(
          "⏳ Executing {Request} [{CorrelationId}] with Timeout policy {Policy}",
          requestName, correlationId, _policyName);

      var pollyContext = new PollyContext($"Franz-{correlationId}");

      try
      {
        TResponse result;

        if (_policy is IAsyncPolicy<TResponse> typedPolicy)
        {
          result = await typedPolicy.ExecuteAsync(async (ctx, ct) => await next(), pollyContext, cancellationToken);
        }
        else if (_policy is IAsyncPolicy untypedPolicy)
        {
          // Execute the downstream chain inside the policy boundary.
          result = default!;
          await untypedPolicy.ExecuteAsync(
              async (ctx, ct) =>
              {
                result = await next().ConfigureAwait(false);
              },
              pollyContext,
              cancellationToken);
        }
        else
        {
          throw new InvalidOperationException(
              $"Policy '{_policyName}' does not support asynchronous execution.");
        }

        stopwatch.Stop();
        resilienceContext.Duration = stopwatch.Elapsed;

        _logger.LogInformation(
            "✅ {Request} [{CorrelationId}] completed within timeout in {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policyName);

        NotifyObservers(resilienceContext);

        return result;
      }
      catch (TimeoutRejectedException ex)
      {
        stopwatch.Stop();
        resilienceContext.Duration = stopwatch.Elapsed;
        resilienceContext.TimeoutOccurred = true;

        _logger.LogError(ex, "⏱️ {Request} [{CorrelationId}] timed out after {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policyName);

        NotifyObservers(resilienceContext);
        throw;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        resilienceContext.Duration = stopwatch.Elapsed;

        _logger.LogError(ex, "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms (policy {Policy})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policyName);

        NotifyObservers(resilienceContext);
        throw;
      }
    }
  }

  private void NotifyObservers(ResilienceContext context)
  {
    ResilienceObserverDispatcher.Notify(_observers, _logger, context.PolicyName, context);
  }
}