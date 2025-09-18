using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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

      if (_env.IsDevelopment())
      {
        // 🔥 Dev: full payload
        _logger.LogInformation("[Pre-{Prefix}] Handling {RequestName} with payload {@Request}",
            prefix, requestType, request);
      }
      else
      {
        // 🟢 Prod: type only
        _logger.LogInformation("[Pre-{Prefix}] Handling {RequestName}",
            prefix, requestType);
      }

      return Task.CompletedTask;
    }
  }
}
