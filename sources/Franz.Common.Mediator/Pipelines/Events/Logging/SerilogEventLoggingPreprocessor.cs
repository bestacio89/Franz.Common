#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Events.Preprocessing;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Events.Logging;

/// <summary>
/// Hardened Serilog pre-processor for events.
/// Establishes the native Guid v7 correlation context before handling begins.
/// </summary>
public sealed class SerilogEventLoggingPreProcessor<TEvent> : IEventPreProcessor<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<SerilogEventLoggingPreProcessor<TEvent>> _logger;

  public SerilogEventLoggingPreProcessor(
      ILogger<SerilogEventLoggingPreProcessor<TEvent>> logger)
  {
    _logger = logger;
  }

  public Task ProcessAsync(TEvent @event, CancellationToken cancellationToken = default)
  {
    var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;


    // This ensures the "Start" log is the chronological anchor for the entire flow.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId();


    using (LogContext.PushProperty("FranzEvent", eventType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogEventLoggingPreProcessor<TEvent>)))
    {
      // Logging the native Guid directly for high-speed indexing.
      _logger.LogInformation("⏳ [Event-Pre] Starting {Event} [{CorrelationId}]",
          eventType, correlationId);
    }

    return Task.CompletedTask;
  }
}