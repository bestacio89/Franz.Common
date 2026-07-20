using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.OpenTelemetry.Core;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
    // Parameter removed from StartEventActivity; ambient MediatorContext.Current is read internally.
    using var activity = FranzActivityFactory.StartEventActivity<TEvent>(_env);

    try
    {
      await next().ConfigureAwait(false);
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.RecordException(ex);
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

      var correlationId = MediatorContext.CorrelationId;
      _logger.LogError(ex, "Event {Event} failed [{CorrelationId}]", typeof(TEvent).Name, correlationId);
      throw;
    }
  }
}