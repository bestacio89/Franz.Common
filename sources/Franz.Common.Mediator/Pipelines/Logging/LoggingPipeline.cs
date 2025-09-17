using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using System;
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
      // ✅ Use runtime type of the request instead of generic TRequest
      var requestName = request?.GetType().FullName ?? typeof(TRequest).FullName ?? typeof(TRequest).Name;

      // ✅ Add correlation ID to tie together all log entries for this request
      var correlationId = Guid.NewGuid();

      _logger.LogInformation("Starting {RequestName} [{CorrelationId}]", requestName, correlationId);

      var start = DateTime.UtcNow;
      try
      {
        var response = await next();

        var duration = DateTime.UtcNow - start;
        _logger.LogInformation(
          "Finished {RequestName} [{CorrelationId}] in {Duration}ms",
          requestName,
          correlationId,
          duration.TotalMilliseconds);

        return response;
      }
      catch (Exception ex)
      {
        _logger.LogError(
          ex,
          "Error handling {RequestName} [{CorrelationId}]",
          requestName,
          correlationId);

        throw;
      }
    }
  }
}
