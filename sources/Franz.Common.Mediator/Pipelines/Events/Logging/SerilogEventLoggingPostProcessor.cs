using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Events.PostProcessing;
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
/// Serilog-powered post-processor for logging event outcomes.
/// Mirrors SerilogLoggingPostProcessor for requests but applies to events.
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
    var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
    CorrelationId.Current = correlationId;

    using (LogContext.PushProperty("FranzEvent", eventType))
    using (LogContext.PushProperty("FranzCorrelationId", correlationId))
    using (LogContext.PushProperty("FranzProcessor", nameof(SerilogEventLoggingPostProcessor<TEvent>)))
    {
      _logger.LogInformation("✅ [Event-Post] {Event} [{CorrelationId}] handled successfully: {@Event}",
          eventType, correlationId, @event);
    }

    return Task.CompletedTask;
  }
}
