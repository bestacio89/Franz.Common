using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Events.Preprocessing;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Events.Logging;
/// <summary>
/// Serilog-powered pre-processor for logging events before handling begins.
/// Mirrors SerilogLoggingPreProcessor for requests but applies to events.
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
    var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
    CorrelationId.Current = correlationId;

    using (LogContext.PushProperty("FranzEvent", eventType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogEventLoggingPreProcessor<TEvent>)))
    {
      _logger.LogInformation("⏳ [Event-Pre] Starting {Event} [{CorrelationId}]",
          eventType, correlationId);
    }

    return Task.CompletedTask;
  }
}
