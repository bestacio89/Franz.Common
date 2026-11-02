using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.OpenTelemetry.Core;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.OpenTelemetry.Pipelines;

public sealed class EventTracingPipeline<TEvent> : IEventPipeline<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<EventTracingPipeline<TEvent>> _logger;
  private readonly ActivitySource _activitySource;
  private readonly IHostEnvironment _env;

  public EventTracingPipeline(
      ILogger<EventTracingPipeline<TEvent>> logger,
      IHostEnvironment env)
      : this(logger, FranzActivityFactory.DefaultSource, env)
  {
  }

  public EventTracingPipeline(
      ILogger<EventTracingPipeline<TEvent>> logger,
      ActivitySource activitySource,
      IHostEnvironment env)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    _env = env ?? throw new ArgumentNullException(nameof(env));
  }

  public async Task HandleAsync(
      TEvent @event,
      Func<Task> next,
      CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    using var activity = FranzActivityFactory.StartEventActivity<TEvent>(_activitySource, _env);

    try
    {
      await next().ConfigureAwait(false);
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.AddException(ex);
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

      var correlationId = MediatorContext.CorrelationId;
      _logger.LogError(ex, "Event {Event} failed [{CorrelationId}]", typeof(TEvent).Name, correlationId);
      throw;
    }
  }
}
