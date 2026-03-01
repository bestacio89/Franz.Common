#nullable enable
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Franz.Common.Mediator.Pipelines.Events.Logging;

/// <summary>
/// Event logging pipeline — logs lifecycle of event handling with hardened Guid v7 correlation and duration.
/// </summary>
public sealed class SerilogEventLoggingPipeline<TEvent> : IEventPipeline<TEvent>
    where TEvent : IEvent
{
  private readonly ILogger<SerilogEventLoggingPipeline<TEvent>> _logger;
  private readonly IHostEnvironment _env;

  public SerilogEventLoggingPipeline(
      ILogger<SerilogEventLoggingPipeline<TEvent>> logger,
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
    var eventName = @event?.GetType().Name ?? typeof(TEvent).Name;

    // Ensure we have a Guid v7 correlation ID. 
    // If one was set by the messaging adapter, it's preserved; otherwise, a new sequential one is created.
    var correlationId = CorrelationId.Ensure();

    var stopwatch = Stopwatch.StartNew();

    // Logging the Guid directly allows Serilog to handle it as a native UUID/Guid type in sinks like Elastic or SQL
    using (_logger.BeginScope(new { CorrelationId = correlationId }))
    {
      try
      {
        if (_env.IsDevelopment())
        {
          _logger.LogInformation(
              "📢 [Event] {EventName} [{CorrelationId}] started with payload {@Event}",
              eventName, correlationId, @event);
        }
        else
        {
          _logger.LogInformation(
              "📢 [Event] {EventName} [{CorrelationId}] started",
              eventName, correlationId);
        }

        await next();

        stopwatch.Stop();

        _logger.LogInformation(
            "✅ [Event] {EventName} [{CorrelationId}] finished in {Elapsed} ms",
            eventName, correlationId, stopwatch.ElapsedMilliseconds);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();

        if (_env.IsDevelopment())
        {
          _logger.LogError(ex,
              "❌ [Event] {EventName} [{CorrelationId}] failed after {Elapsed} ms",
              eventName, correlationId, stopwatch.ElapsedMilliseconds);
        }
        else
        {
          _logger.LogError(
              "❌ [Event] {EventName} [{CorrelationId}] failed after {Elapsed} ms with {ErrorMessage}",
              eventName, correlationId, stopwatch.ElapsedMilliseconds, ex.Message);
        }

        throw;
      }
    }
  }
}