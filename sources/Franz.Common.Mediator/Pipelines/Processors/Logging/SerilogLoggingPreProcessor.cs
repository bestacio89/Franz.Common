using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class SerilogLoggingPreProcessor<TRequest> : IPreProcessor<TRequest>
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
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

      using (LogContext.PushProperty("FranzRequest", requestType))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      using (LogContext.PushProperty("FranzProcessor", nameof(SerilogLoggingPreProcessor<TRequest>)))
      {
        _logger.LogInformation("⏳ [Pre] Starting {Request} [{CorrelationId}]",
            requestType, correlationId);
      }

      return Task.CompletedTask;
    }
  }
}
