using Franz.Common.Mediator.Pipelines; // 👈 your shared CorrelationId
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class LoggingPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
  {
    private readonly ILogger<LoggingPostProcessor<TRequest, TResponse>> _logger;
    private readonly IHostEnvironment _env;

    public LoggingPostProcessor(
      ILogger<LoggingPostProcessor<TRequest, TResponse>> logger,
      IHostEnvironment env)
    {
      _logger = logger;
      _env = env;
    }

    public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default)
    {
      var requestType = request?.GetType().Name ?? typeof(TRequest).Name;
      string prefix = requestType.EndsWith("Command", StringComparison.OrdinalIgnoreCase)
          ? "Command"
          : requestType.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
              ? "Query"
              : "Request";

      // ✅ Pull from shared CorrelationId
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

      using (_logger.BeginScope(new { CorrelationId = correlationId }))
      {
        if (_env.IsDevelopment())
        {
          _logger.LogInformation(
            "[Post-{Prefix}] {RequestName} [{CorrelationId}] produced response {@Response}",
            prefix, requestType, correlationId, response);
        }
        else
        {
          _logger.LogInformation(
            "[Post-{Prefix}] {RequestName} [{CorrelationId}] completed successfully",
            prefix, requestType, correlationId);
        }
      }

      return Task.CompletedTask;
    }
  }
}
