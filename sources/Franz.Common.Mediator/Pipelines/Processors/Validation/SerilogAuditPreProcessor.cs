#nullable enable
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Validation;

/// <summary>
/// Hardened Serilog audit pre-processor.
/// Sets the chronological anchor (Guid v7) for the validation phase.
/// </summary>
public sealed class SerilogAuditPreProcessor<TRequest> : IPreProcessor<TRequest>
{
  private readonly ILogger<SerilogAuditPreProcessor<TRequest>> _logger;

  public SerilogAuditPreProcessor(
      ILogger<SerilogAuditPreProcessor<TRequest>> logger)
  {
    _logger = logger;
  }

  public Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
  {
    var requestType = request?.GetType().Name ?? typeof(TRequest).Name;

    // If this is the entry point, we create the timestamped ID here.
    var correlationId = CorrelationId.Ensure();
    CorrelationId.Current = correlationId;

    using (LogContext.PushProperty("FranzRequest", requestType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogAuditPreProcessor<TRequest>)))
    {
      // Log native Guid—optimized for UUID indexing in modern log storage.
      _logger.LogInformation("🔎 [Audit-Pre] Validating {Request} [{CorrelationId}]",
          requestType, correlationId);
    }

    return Task.CompletedTask;
  }
}