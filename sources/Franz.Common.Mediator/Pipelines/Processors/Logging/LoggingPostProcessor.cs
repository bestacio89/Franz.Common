#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging;

/// <summary>
/// Hardened Post-Processor for logging request outcomes.
/// Finalizes the lineage using native Guid v7 correlation.
/// </summary>
public sealed class LoggingPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
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

    // BAZOOKA REFACTOR: Fetch the existing Guid v7.
    // In a Post-Processor, we expect the ID to already exist from the Pre-Processor or Pipeline.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

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
        // Clean production logging with native Guid
        _logger.LogInformation(
            "[Post-{Prefix}] {RequestName} [{CorrelationId}] completed successfully",
            prefix, requestType, correlationId);
      }
    }

    return Task.CompletedTask;
  }
}