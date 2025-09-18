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
      var correlationId = Guid.NewGuid().ToString("N");
      var stopwatch = Stopwatch.StartNew();

      using (LogContext.PushProperty("FranzRequest", requestName))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      {
        _logger.LogInformation("▶️ Handling {Request}", requestName);

        try
        {
          var response = await next();

          stopwatch.Stop();
          _logger.LogInformation("✅ {Request} completed in {Elapsed}ms",
              requestName, stopwatch.ElapsedMilliseconds);

          return response;
        }
        catch (Exception ex)
        {
          stopwatch.Stop();
          _logger.LogError(ex, "❌ {Request} failed after {Elapsed}ms",
              requestName, stopwatch.ElapsedMilliseconds);
          throw;
        }
      }
    }
  }
}
