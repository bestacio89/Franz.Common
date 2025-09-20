using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Polly.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Serilog.Context;
using System.Diagnostics;

namespace Franz.Common.Mediator.Polly.Pipelines
{
  public class PollyBulkheadPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<TRequest> _logger;

    public PollyBulkheadPipeline(
        IReadOnlyPolicyRegistry<string> registry,
        IOptions<PollyBulkheadPipelineOptions> options,
        ILogger<TRequest> logger)
    {
      _policy = registry.Get<IAsyncPolicy>(options.Value.PolicyName);
      _logger = logger;
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

      using (LogContext.PushProperty("FranzRequest", requestName))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      using (LogContext.PushProperty("FranzPipeline", nameof(PollyBulkheadPipeline<TRequest, TResponse>)))
      using (LogContext.PushProperty("FranzPolicy", _policy.PolicyKey))
      {
        _logger.LogInformation("▶️ Executing {Request} [{CorrelationId}] with Bulkhead {Policy}",
            requestName, correlationId, _policy.PolicyKey);

        try
        {
          var result = await _policy.ExecuteAsync(
              async ct => await next(),
              cancellationToken);

          stopwatch.Stop();
          _logger.LogInformation("✅ {Request} [{CorrelationId}] completed through Bulkhead in {Elapsed}ms (policy {Policy})",
              requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);

          return result;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogError(ex, "❌ {Request} [{CorrelationId}] rejected/faulted in Bulkhead after {Elapsed}ms (policy {Policy})",
              requestName, correlationId, stopwatch.ElapsedMilliseconds, _policy.PolicyKey);
          throw;
        }
      }
    }
  }
}
