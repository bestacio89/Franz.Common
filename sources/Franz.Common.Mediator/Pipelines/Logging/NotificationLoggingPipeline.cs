#nullable enable
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Franz.Common.Mediator.Pipelines.Logging;

/// <summary>
/// Hardened logging pipeline for Mediator notifications.
/// Ensures native Guid v7 correlation is maintained across broadcast events.
/// </summary>
public sealed class NotificationLoggingPipeline<TNotification> : INotificationPipeline<TNotification>
    where TNotification : Messages.INotification
{
  private readonly ILogger<NotificationLoggingPipeline<TNotification>> _logger;
  private readonly IHostEnvironment _env;

  public NotificationLoggingPipeline(
      ILogger<NotificationLoggingPipeline<TNotification>> logger,
      IHostEnvironment env)
  {
    _logger = logger;
    _env = env;
  }

  public async Task Handle(
      TNotification notification,
      Func<Task> next,
      CancellationToken cancellationToken = default)
  {
    var notificationName = typeof(TNotification).Name;

    // Notifications are usually secondary effects; this keeps them linked to the primary intent.
    var correlationId = CorrelationId.Ensure();
    CorrelationId.Current = correlationId;

    var stopwatch = Stopwatch.StartNew();

    // Scope with native Guid for optimized search in Seq/ELK
    using (_logger.BeginScope(new { CorrelationId = correlationId }))
    {
      try
      {
        if (_env.IsDevelopment())
        {
          _logger.LogInformation(
              "[Notification] {NotificationName} [{CorrelationId}] started with payload {@Notification}",
              notificationName, correlationId, notification);
        }
        else
        {
          _logger.LogInformation(
              "[Notification] {NotificationName} [{CorrelationId}] started",
              notificationName, correlationId);
        }

        await next();

        stopwatch.Stop();

        if (_env.IsDevelopment())
        {
          _logger.LogInformation(
              "[Notification] {NotificationName} [{CorrelationId}] finished in {Elapsed} ms with payload {@Notification}",
              notificationName, correlationId, stopwatch.ElapsedMilliseconds, notification);
        }
        else
        {
          _logger.LogInformation(
              "[Notification] {NotificationName} [{CorrelationId}] finished in {Elapsed} ms",
              notificationName, correlationId, stopwatch.ElapsedMilliseconds);
        }
      }
      catch (Exception ex)
      {
        stopwatch.Stop();

        if (_env.IsDevelopment())
        {
          _logger.LogError(ex,
              "[Notification] {NotificationName} [{CorrelationId}] failed after {Elapsed} ms with payload {@Notification}",
              notificationName, correlationId, stopwatch.ElapsedMilliseconds, notification);
        }
        else
        {
          _logger.LogError(
              "[Notification] {NotificationName} [{CorrelationId}] failed after {Elapsed} ms with {ErrorMessage}",
              notificationName, correlationId, stopwatch.ElapsedMilliseconds, ex.Message);
        }

        throw;
      }
    }
  }
}