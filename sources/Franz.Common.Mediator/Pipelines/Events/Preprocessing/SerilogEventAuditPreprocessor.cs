#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Events.Preprocessing;

/// <summary>
/// Hardened Serilog audit pre-processor for events.
/// Uses native Guid v7 to ensure the audit trail starts with a sortable, chronological anchor.
/// </summary>
public sealed class SerilogEventAuditPreProcessor<TEvent> : IEventPreProcessor<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<SerilogEventAuditPreProcessor<TEvent>> _logger;

  public SerilogEventAuditPreProcessor(
      ILogger<SerilogEventAuditPreProcessor<TEvent>> logger)
  {
    _logger = logger;
  }

  public Task ProcessAsync(TEvent @event, CancellationToken cancellationToken = default)
  {
    var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;

    // BAZOOKA REFACTOR: Fetch or establish the native Guid v7 context.
    // This ensures the audit trail is physically ordered in your log storage.
    var correlationId = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId(); 

   
    using (LogContext.PushProperty("FranzEvent", eventType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogEventAuditPreProcessor<TEvent>)))
    {
      // Log native Guid for optimized UUID indexing in Seq/Elasticsearch
      _logger.LogInformation("🔎 [Audit-Pre] Handling {Event} [{CorrelationId}]",
          eventType, correlationId);
    }

    return Task.CompletedTask;
  }
}