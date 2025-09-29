using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Franz.Common.Mediator.OpenTelemetry.Pipelines;

public sealed class EventTracingPipeline<TEvent> : IEventPipeline<TEvent>
  where TEvent : IEvent
{
  // You can also inject this via DI if you prefer.
  private static readonly ActivitySource ActivitySource = new("Franz.Mediator.Events");

  private readonly ILogger<EventTracingPipeline<TEvent>> _logger;

  public EventTracingPipeline(ILogger<EventTracingPipeline<TEvent>> logger)
  {
    _logger = logger;
  }

  public async Task HandleAsync(
    TEvent @event,
    Func<Task> next,
    CancellationToken cancellationToken = default)
  {
    var eventName = typeof(TEvent).Name;
    var correlationId = CorrelationId.Current ?? Guid.NewGuid().ToString("N");

    using var activity = ActivitySource.StartActivity(
      $"Event:{eventName}",
      ActivityKind.Consumer);

    // Tag the span/activity (safe if activity is null)
    activity?.SetTag("franz.event.name", eventName);
    activity?.SetTag("franz.event.type", typeof(TEvent).FullName ?? eventName);
    activity?.SetTag("franz.correlation_id", correlationId);

    try
    {
      _logger.LogDebug("Handling event {EventName} [{CorrelationId}]", eventName, correlationId);

      await next();

      activity?.SetStatus(ActivityStatusCode.Ok);
      _logger.LogInformation("Handled event {EventName} [{CorrelationId}]", eventName, correlationId);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("exception.type", ex.GetType().FullName);
      activity?.SetTag("exception.message", ex.Message);
      activity?.SetTag("exception.stacktrace", ex.StackTrace);

      _logger.LogError(ex, "Event {EventName} [{CorrelationId}] failed", eventName, correlationId);
      throw;
    }
  }
}
