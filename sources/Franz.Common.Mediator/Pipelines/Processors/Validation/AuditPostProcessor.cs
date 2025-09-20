using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Processors.Validation
{
  public class AuditPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
  {
    private readonly ILogger<AuditPostProcessor<TRequest, TResponse>> _logger;
    private readonly IHostEnvironment _env;

    public AuditPostProcessor(
      ILogger<AuditPostProcessor<TRequest, TResponse>> logger,
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
        // 🔥 Dev: full detail
        _logger.LogInformation("[Audit-{Prefix}] {RequestName} -> {@Response}",
            prefix, requestType, response);
      }
      else
      {
        // 🟢 Prod: slim info
        _logger.LogInformation("[Audit-{Prefix}] {RequestName} completed",
            prefix, requestType);
      }

      return Task.CompletedTask;
    }
  }
}
