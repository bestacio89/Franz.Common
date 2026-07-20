#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Events.PostProcessing;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Events.Logging;

/// <summary>
/// Hardened Serilog post-processor for logging event outcomes.
/// Completes the audit trail using native Guid v7 correlation.
/// </summary>
public sealed class SerilogEventLoggingPostProcessor<TEvent> : IEventPostProcessor<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<SerilogEventLoggingPostProcessor<TEvent>> _logger;

  public SerilogEventLoggingPostProcessor(
      ILogger<SerilogEventLoggingPostProcessor<TEvent>> logger)
  {
    _logger = logger;
  }

  public Task ProcessAsync(TEvent @event, CancellationToken cancellationToken = default)
  {
    var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;

    
    // This ensures the "Success" log is bitwise-linked to the "Start" log.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId(); // Guarantees we have a v7 Guid

    using (LogContext.PushProperty("FranzEvent", eventType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogEventLoggingPostProcessor<TEvent>)))
    {
      // Logging the native Guid directly for high-speed indexing in Seq/ELK/SQL.
      _logger.LogInformation("✅ [Event-Post] {Event} [{CorrelationId}] handled successfully: {@Event}",
          eventType, correlationId, @event);
    }

    return Task.CompletedTask;
  }
}