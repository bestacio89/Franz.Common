#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging;

/// <summary>
/// Hardened Serilog pre-processor for establishing request identity.
/// Sets the native Guid v7 correlation context before the handler executes.
/// </summary>
public sealed class SerilogLoggingPreProcessor<TRequest> : IPreProcessor<TRequest>
{
  private readonly ILogger<SerilogLoggingPreProcessor<TRequest>> _logger;

  public SerilogLoggingPreProcessor(
      ILogger<SerilogLoggingPreProcessor<TRequest>> logger)
  {
    _logger = logger;
  }

  public Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
  {
    var requestType = request?.GetType().Name ?? typeof(TRequest).Name;

    // Ensure() creates the Guid v7 that links all downstream Serilog properties.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

    using (LogContext.PushProperty("FranzRequest", requestType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogLoggingPreProcessor<TRequest>)))
    {
      // Log native Guid directly—optimized for modern log sinks (Seq, ELK, etc.)
      _logger.LogInformation("⏳ [Pre] Starting {Request} [{CorrelationId}]",
          requestType, correlationId);
    }

    return Task.CompletedTask;
  }
}