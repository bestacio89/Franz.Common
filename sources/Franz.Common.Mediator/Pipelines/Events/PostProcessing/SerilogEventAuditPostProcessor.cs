#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Events.PostProcessing;

/// <summary>
/// Hardened Serilog post-processor for auditing events.
/// Uses native Guid v7 correlation for strictly ordered audit trails.
/// </summary>
public sealed class SerilogEventAuditPostProcessor<TEvent> : IEventPostProcessor<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<SerilogEventAuditPostProcessor<TEvent>> _logger;
  private readonly IHostEnvironment _env;

  public SerilogEventAuditPostProcessor(
      ILogger<SerilogEventAuditPostProcessor<TEvent>> logger,
      IHostEnvironment env)
  {
    _logger = logger;
    _env = env;
  }

  public Task ProcessAsync(TEvent @event, CancellationToken cancellationToken = default)
  {
    var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;

    // instead of creating a random string "N" ID.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();

    

    using (LogContext.PushProperty("FranzEvent", eventType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogEventAuditPostProcessor<TEvent>)))
    {
      if (_env.IsDevelopment())
      {
        _logger.LogInformation("✅ [Audit-Event] {Event} [{CorrelationId}] details {@Event}",
            eventType, correlationId, @event);
      }
      else
      {
        // In production, we log the Guid directly to allow native UUID indexing in sinks
        _logger.LogInformation("✅ [Audit-Event] {Event} [{CorrelationId}] handled successfully",
            eventType, correlationId);
      }
    }

    return Task.CompletedTask;
  }
}