#nullable enable
using Franz.Common.Mediator.Context;
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
  private readonly IsPolicy _policy;
  private readonly string _policyName;
  private readonly ILogger<TRequest> _logger;
  private readonly IEnumerable<IResilienceObserver> _observers;

  public PollyRetryPipeline(
      IReadOnlyPolicyRegistry<string> registry,
      IOptions<PollyRetryPipelineOptions> options,
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

    // BAZOOKA REFACTOR: Bridge to the native Guid v7 identity.
    var correlationId = MediatorContext.EnsureCorrelationId();

    var stopwatch = Stopwatch.StartNew();
    var resilienceContext = new Context.ResilienceContext
    {
      PolicyName = _policyName,
      RetryCount = 0
    };

    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(PollyRetryPipeline<TRequest, TResponse>)))
    using (LogContext.PushProperty("FranzPolicy", _policyName))
    {
      _logger.LogInformation(
          "▶️ Executing {Request} [{CorrelationId}] with Retry policy {Policy}",
          requestName, correlationId, _policyName);

      // Pass the Guid-based identity into Polly's internal context
      var pollyContext = new PollyContext($"Franz-{correlationId}");

      try
      {
        TResponse result;
        var attemptCount = 0;

        async Task<TResponse> ExecuteAttemptAsync()
        {
          attemptCount++;
          resilienceContext.RetryCount = attemptCount - 1;

          if (attemptCount > 1)
          {
            _logger.LogWarning(
                "🔁 {Request} [{CorrelationId}] retry attempt {RetryCount} (policy {Policy})",
                requestName, correlationId, resilienceContext.RetryCount, _policyName);
          }

          return await next().ConfigureAwait(false);
        }

        if (_policy is IAsyncPolicy<TResponse> typedPolicy)
        {
          result = await typedPolicy.ExecuteAsync(
              (_, _) => ExecuteAttemptAsync(),
              pollyContext,
              cancellationToken);
        }
        else if (_policy is IAsyncPolicy untypedPolicy)
        {
          // Capture the response inside Polly so every retry attempt owns exactly one downstream execution.
          result = default!;
          await untypedPolicy.ExecuteAsync(
              async (_, _) =>
              {
                result = await ExecuteAttemptAsync().ConfigureAwait(false);
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
            "✅ {Request} [{CorrelationId}] succeeded after {Elapsed}ms (policy {Policy}, retries={RetryCount})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policyName, resilienceContext.RetryCount);

        NotifyObservers(resilienceContext);
        return result;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        resilienceContext.Duration = stopwatch.Elapsed;

        _logger.LogError(ex,
            "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms (policy {Policy}, retries={RetryCount})",
            requestName, correlationId, stopwatch.ElapsedMilliseconds, _policyName, resilienceContext.RetryCount);

        NotifyObservers(resilienceContext);
        throw;
      }
    }
  }

  private void NotifyObservers(Context.ResilienceContext context)
  {
    ResilienceObserverDispatcher.Notify(_observers, _logger, context.PolicyName, context);
  }
}