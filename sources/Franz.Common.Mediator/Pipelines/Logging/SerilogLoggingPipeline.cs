using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Franz.Common.Mediator.Pipelines.Logging
{
  public class SerilogLoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly ILogger<TRequest> _logger;

    public SerilogLoggingPipeline(ILogger<TRequest> logger)
    {
      _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
      var requestName = request?.GetType().Name ?? typeof(TRequest).Name;

      // ✅ Reuse existing correlation ID if already set
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId; // ensure it's available downstream

      var stopwatch = Stopwatch.StartNew();

      using (LogContext.PushProperty("FranzRequest", requestName))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      {
        _logger.LogInformation("▶️ Handling {Request} [{CorrelationId}]",
            requestName, correlationId);

        try
        {
          var response = await next();

          stopwatch.Stop();
          _logger.LogInformation("✅ {Request} [{CorrelationId}] completed in {Elapsed}ms",
              requestName, correlationId, stopwatch.ElapsedMilliseconds);

          return response;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogError(ex, "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms",
              requestName, correlationId, stopwatch.ElapsedMilliseconds);
          throw;
        }
      }
    }
  }

}
