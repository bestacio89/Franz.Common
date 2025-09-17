using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class LoggingPreProcessor<TRequest> : IPreProcessor<TRequest>
  {
    private readonly ILogger<LoggingPreProcessor<TRequest>> _logger;

    public LoggingPreProcessor(ILogger<LoggingPreProcessor<TRequest>> logger)
    {
      _logger = logger;
    }

    public Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
    {
      var requestType = request?.GetType().Name ?? typeof(TRequest).Name;
      string prefix = requestType.EndsWith("Command", StringComparison.OrdinalIgnoreCase)
          ? "Command"
          : requestType.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
              ? "Query"
              : "Request";

      _logger.LogInformation("[Pre-{Prefix}] Handling {RequestName}", prefix, requestType);

      return Task.CompletedTask;
    }
  }
}
