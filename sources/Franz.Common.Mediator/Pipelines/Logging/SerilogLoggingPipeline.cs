#nullable enable
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Franz.Common.Mediator.Pipelines.Logging;

/// <summary>
/// Hardened Serilog logging pipeline.
/// Enforces native Guid v7 correlation for structured logging and high-speed diagnostics.
/// </summary>
public sealed class SerilogLoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly ILogger<TRequest> _logger;

  public SerilogLoggingPipeline(ILogger<TRequest> logger)
  {
    _logger = logger;
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    var requestName = request?.GetType().Name ?? typeof(TRequest).Name;

 
    // This ensures the correlation ID is a sortable, timestamped binary.
    var correlationId = CorrelationId.Ensure();
    CorrelationId.Current = correlationId;

    var stopwatch = Stopwatch.StartNew();

    // Pushing native Guid to Serilog context for optimized UUID indexing in sinks.
    using (LogContext.PushProperty("FranzRequest", requestName))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzPipeline", nameof(SerilogLoggingPipeline<TRequest, TResponse>)))
    {
      _logger.LogInformation("▶️ Handling {Request} [{CorrelationId}]",
          requestName, correlationId);

      try
      {
        var response = await next();

        stopwatch.Stop();
        _logger.LogInformation("✅ {Request} [{CorrelationId}] completed in {Elapsed}ms",
            requestName, correlationId, stopwatch.ElapsedMilliseconds);

        return response;
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        _logger.LogError(ex, "❌ {Request} [{CorrelationId}] failed after {Elapsed}ms",
            requestName, correlationId, stopwatch.ElapsedMilliseconds);
        throw;
      }
    }
  }
}