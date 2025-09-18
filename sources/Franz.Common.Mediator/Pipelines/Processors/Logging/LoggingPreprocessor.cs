using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class LoggingPreProcessor<TRequest> : IPreProcessor<TRequest>
  {
    private readonly ILogger<LoggingPreProcessor<TRequest>> _logger;
    private readonly IHostEnvironment _env;

    public LoggingPreProcessor(
      ILogger<LoggingPreProcessor<TRequest>> logger,
      IHostEnvironment env)
    {
      _logger = logger;
      _env = env;
    }

    public Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
    {
      var requestType = request?.GetType().Name ?? typeof(TRequest).Name;
      string prefix = requestType.EndsWith("Command", StringComparison.OrdinalIgnoreCase)
          ? "Command"
          : requestType.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
              ? "Query"
              : "Request";

      // ✅ Reuse or generate correlation ID
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

      using (_logger.BeginScope(new { CorrelationId = correlationId }))
      {
        if (_env.IsDevelopment())
        {
          // 🔥 Dev: full payload
          _logger.LogInformation(
            "[Pre-{Prefix}] Handling {RequestName} [{CorrelationId}] with payload {@Request}",
            prefix, requestType, correlationId, request);
        }
        else
        {
          // 🟢 Prod: minimal
          _logger.LogInformation(
            "[Pre-{Prefix}] Handling {RequestName} [{CorrelationId}]",
            prefix, requestType, correlationId);
        }
      }

      return Task.CompletedTask;
    }
  }
}
