#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging; // if you still need it
using Franz.Common.Mediator.Validation.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Franz.Common.Mediator.Pipelines.Events.Logging;

/// <summary>
/// Event logging pipeline — logs lifecycle with rich MediatorExecutionContext.
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

    // === NEW: Use unified context ===
    var context = MediatorContext.Current;
    MediatorContext.EnsureCorrelationId(); // Guarantees we have a v7 Guid

    var stopwatch = Stopwatch.StartNew();

    using (_logger.BeginScope(new
    {
      CorrelationId = context.CorrelationId,
      UserId = context.UserId,
      TenantId = context.TenantId
    }))
    {
      try
      {
        if (_env.IsDevelopment())
        {
          _logger.LogInformation(
              "📢 [Event] {EventName} [{CorrelationId}] started with payload {@Event} | User={UserId} | Tenant={TenantId}",
              eventName,
              context.CorrelationId,
              @event,
              context.UserId,
              context.TenantId);
        }
        else
        {
          _logger.LogInformation(
              "📢 [Event] {EventName} [{CorrelationId}] started",
              eventName, context.CorrelationId);
        }

        await next();

        stopwatch.Stop();

        _logger.LogInformation(
            "✅ [Event] {EventName} [{CorrelationId}] finished in {Elapsed} ms",
            eventName, context.CorrelationId, stopwatch.ElapsedMilliseconds);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();

        if (_env.IsDevelopment())
        {
          _logger.LogError(ex,
              "❌ [Event] {EventName} [{CorrelationId}] failed after {Elapsed} ms",
              eventName, context.CorrelationId, stopwatch.ElapsedMilliseconds);
        }
        else
        {
          _logger.LogError(
              "❌ [Event] {EventName} [{CorrelationId}] failed after {Elapsed} ms",
              eventName, context.CorrelationId, stopwatch.ElapsedMilliseconds);
        }

        throw;
      }
    }
  }
}