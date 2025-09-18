using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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

      if (_env.IsDevelopment())
      {
        // 🔥 Dev mode: log full response payload
        _logger.LogInformation(
          "[Post-{Prefix}] {RequestName} produced response {@Response}",
          prefix, requestType, response);
      }
      else
      {
        // 🟢 Prod mode: only log status
        _logger.LogInformation(
          "[Post-{Prefix}] {RequestName} completed successfully",
          prefix, requestType);
      }

      return Task.CompletedTask;
    }
  }
}
