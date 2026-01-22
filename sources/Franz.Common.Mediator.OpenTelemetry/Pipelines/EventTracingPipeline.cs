using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.OpenTelemetry.Core;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Franz.Common.Mediator.OpenTelemetry.Pipelines;

public sealed class EventTracingPipeline<TEvent> : IEventPipeline<TEvent>
  where TEvent : IEvent
{
  private readonly ILogger<EventTracingPipeline<TEvent>> _logger;
  private readonly IHostEnvironment _env;

  public EventTracingPipeline(
    ILogger<EventTracingPipeline<TEvent>> logger,
    IHostEnvironment env)
  {
    _logger = logger;
    _env = env;
  }

  public async Task HandleAsync(
    TEvent @event,
    Func<Task> next,
    CancellationToken cancellationToken = default)
  {
    var ctx = MediatorContext.Current;

    using var activity =
      FranzActivityFactory.StartEventActivity<TEvent>(_env, ctx);

    try
    {
      await next();
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.AddException(ex);
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      _logger.LogError(ex, "Event {Event} failed [{CorrelationId}]",
        typeof(TEvent).Name, ctx.CorrelationId);
      throw;
    }
  }
}
