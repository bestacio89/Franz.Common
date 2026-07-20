#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging;

/// <summary>
/// Hardened Pre-Processor for establishing request identity.
/// Anchors the entire request to a native Guid v7 for chronological traceability.
/// </summary>
public sealed class LoggingPreProcessor<TRequest> : IPreProcessor<TRequest>
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

    // Ensure() establishes the 128-bit Guid v7 that will travel through 
    // Pipelines, Sagas, and Domain Events.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

    using (_logger.BeginScope(new { CorrelationId = correlationId }))
    {
      if (_env.IsDevelopment())
      {
        _logger.LogInformation(
            "[Pre-{Prefix}] Handling {RequestName} [{CorrelationId}] with payload {@Request}",
            prefix, requestType, correlationId, request);
      }
      else
      {
        _logger.LogInformation(
            "[Pre-{Prefix}] Handling {RequestName} [{CorrelationId}]",
            prefix, requestType, correlationId);
      }
    }

    return Task.CompletedTask;
  }
}