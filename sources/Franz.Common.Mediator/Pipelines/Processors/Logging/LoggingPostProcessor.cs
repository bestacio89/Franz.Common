using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class LoggingPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
  {
    private readonly ILogger<LoggingPostProcessor<TRequest, TResponse>> _logger;

    public LoggingPostProcessor(ILogger<LoggingPostProcessor<TRequest, TResponse>> logger)
    {
      _logger = logger;
    }

    public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default)
    {
      var requestType = request?.GetType().Name ?? typeof(TRequest).Name;
      string prefix = requestType.EndsWith("Command", StringComparison.OrdinalIgnoreCase)
          ? "Command"
          : requestType.EndsWith("Query", StringComparison.OrdinalIgnoreCase)
              ? "Query"
              : "Request";

      _logger.LogInformation("[Post-{Prefix}] {RequestName} produced response {Response}",
          prefix, requestType, response);

      return Task.CompletedTask;
    }
  }
}
