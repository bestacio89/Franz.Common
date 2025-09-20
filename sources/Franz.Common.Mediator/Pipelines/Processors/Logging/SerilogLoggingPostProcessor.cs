using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class SerilogLoggingPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
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
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

      using (LogContext.PushProperty("FranzRequest", requestType))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      using (LogContext.PushProperty("FranzProcessor", nameof(SerilogLoggingPostProcessor<TRequest, TResponse>)))
      {
        _logger.LogInformation("✅ [Post] {Request} [{CorrelationId}] produced {@Response}",
            requestType, correlationId, response);
      }

      return Task.CompletedTask;
    }
  }
}
