using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Logging
{
  public class LoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly ILogger<LoggingPipeline<TRequest, TResponse>> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline<TRequest, TResponse>> logger)
    {
      _logger = logger;
    }

    public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
    {
      // ✅ Use runtime type to catch *concrete* command/query names
      var requestName = request?.GetType().Name ?? typeof(TRequest).Name;

      // Optional: tag as Command/Query if suffix matches
      var prefix = requestName.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
        ? "Query"
        : requestName.EndsWith("Command", StringComparison.OrdinalIgnoreCase)
          ? "Command"
          : "Request";

      var correlationId = Guid.NewGuid().ToString("N");
      var stopwatch = Stopwatch.StartNew();

      _logger.LogInformation("[{Prefix}] {RequestName} [{CorrelationId}] started",
        prefix, requestName, correlationId);

      try
      {
        var response = await next();

        stopwatch.Stop();
        _logger.LogInformation("[{Prefix}] {RequestName} [{CorrelationId}] finished in {Elapsed} ms",
          prefix, requestName, correlationId, stopwatch.ElapsedMilliseconds);

        return response;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        _logger.LogError(ex,
          "[{Prefix}] {RequestName} [{CorrelationId}] failed after {Elapsed} ms",
          prefix, requestName, correlationId, stopwatch.ElapsedMilliseconds);

        throw;
      }
    }
  }
}
