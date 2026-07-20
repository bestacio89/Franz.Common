#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging;

/// <summary>
/// Hardened Serilog post-processor for request outcomes.
/// Finalizes the structured audit trail using native Guid v7 correlation.
/// </summary>
public sealed class SerilogLoggingPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
{
  private readonly ILogger<SerilogLoggingPostProcessor<TRequest, TResponse>> _logger;

  public SerilogLoggingPostProcessor(
      ILogger<SerilogLoggingPostProcessor<TRequest, TResponse>> logger)
  {
    _logger = logger;
  }

  public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default)
  {
    var requestType = request?.GetType().Name ?? typeof(TRequest).Name;

    // This keeps the "Success" log bitwise-linked to the "Start" log.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

    using (LogContext.PushProperty("FranzRequest", requestType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogLoggingPostProcessor<TRequest, TResponse>)))
    {
      // Log the native Guid—Serilog sinks will index this as a UUID/Guid type, not a string.
      _logger.LogInformation("✅ [Post] {Request} [{CorrelationId}] produced {@Response}",
          requestType, correlationId, response);
    }

    return Task.CompletedTask;
  }
}