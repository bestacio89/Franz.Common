#nullable enable
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Validation;

/// <summary>
/// Hardened Serilog audit post-processor.
/// Finalizes the chronological audit trail using native Guid v7 correlation.
/// </summary>
public sealed class SerilogAuditPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
{
  private readonly ILogger<SerilogAuditPostProcessor<TRequest, TResponse>> _logger;
  private readonly IHostEnvironment _env;

  public SerilogAuditPostProcessor(
      ILogger<SerilogAuditPostProcessor<TRequest, TResponse>> logger,
      IHostEnvironment env)
  {
    _logger = logger;
    _env = env;
  }

  public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default)
  {
    var requestType = request?.GetType().Name ?? typeof(TRequest).Name;
    // BAZOOKA REFACTOR: Retrieve the existing Guid v7 from the context.
    // We use Ensure() to guarantee we have a valid lineage for the final audit entry.
    var correlationId = CorrelationId.Ensure();

    using (LogContext.PushProperty("FranzRequest", requestType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogAuditPostProcessor<TRequest, TResponse>)))
    {
      if (_env.IsDevelopment())
      {
        // In Dev, we log the full response for deep debugging
        _logger.LogInformation("✅ [Audit-Post] {Request} [{CorrelationId}] response {@Response}",
            requestType, correlationId, response);
      }
      else
      {
        // In Prod, we log the native Guid directly for high-speed UUID indexing
        _logger.LogInformation("✅ [Audit-Post] {Request} [{CorrelationId}] completed successfully",
            requestType, correlationId);
      }
    }

    return Task.CompletedTask;
  }
}