using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Franz.Common.Mediator.Pipelines.Events.PostProcessing;
/// <summary>
/// Serilog-powered post-processor for auditing events.
/// Mirrors SerilogAuditPostProcessor for commands/queries but applies to events.
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
      var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");
      CorrelationId.Current = correlationId;

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
          _logger.LogInformation("✅ [Audit-Event] {Event} [{CorrelationId}] handled successfully",
              eventType, correlationId);
        }
      }

      return Task.CompletedTask;
    }
  }

