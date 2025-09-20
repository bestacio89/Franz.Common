using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Processors.Validation
{
  public class SerilogAuditPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
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
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

      using (LogContext.PushProperty("FranzRequest", requestType))
      using (LogContext.PushProperty("FranzCorrelationId", correlationId))
      using (LogContext.PushProperty("FranzProcessor", nameof(SerilogAuditPostProcessor<TRequest, TResponse>)))
      {
        if (_env.IsDevelopment())
        {
          _logger.LogInformation("✅ [Audit-Post] {Request} [{CorrelationId}] response {@Response}",
              requestType, correlationId, response);
        }
        else
        {
          _logger.LogInformation("✅ [Audit-Post] {Request} [{CorrelationId}] completed successfully",
              requestType, correlationId);
        }
      }

      return Task.CompletedTask;
    }
  }
}
