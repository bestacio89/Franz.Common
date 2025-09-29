using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Events.PostProcessing;
/// <summary>
/// Post-processor for auditing events after they are dispatched and handled.
/// Mirrors the AuditPostProcessor for commands/queries.
/// </summary>
public sealed class EventAuditPostProcessor<TEvent> : IEventPostProcessor<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<EventAuditPostProcessor<TEvent>> _logger;
  private readonly IHostEnvironment _env;

  public EventAuditPostProcessor(
    ILogger<EventAuditPostProcessor<TEvent>> logger,
    IHostEnvironment env)
  {
    _logger = logger;
    _env = env;
  }

  public Task ProcessAsync(TEvent @event, CancellationToken cancellationToken = default)
  {
    var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;

    if (_env.IsDevelopment())
    {
      // 🔥 Dev: full detail
      _logger.LogInformation("[Audit-Event] {EventName} -> {@Event}",
          eventType, @event);
    }
    else
    {
      // 🟢 Prod: slim info
      _logger.LogInformation("[Audit-Event] {EventName} handled", eventType);
    }

    return Task.CompletedTask;
  }
}