using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Logging
{
  public class NotificationLoggingPipeline<TNotification> : INotificationPipeline<TNotification>
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
      var start = DateTime.UtcNow;

      if (_env.IsDevelopment())
      {
        // 🔥 Dev mode: log full payload
        _logger.LogInformation("Starting notification {NotificationName} with payload {@Notification}",
          notificationName, notification);
      }
      else
      {
        // 🟢 Prod mode: minimal info
        _logger.LogInformation("Starting notification {NotificationName}", notificationName);
      }

      try
      {
        await next();

        var duration = DateTime.UtcNow - start;

        if (_env.IsDevelopment())
        {
          _logger.LogInformation("Finished notification {NotificationName} in {Duration}ms with payload {@Notification}",
              notificationName, duration.TotalMilliseconds, notification);
        }
        else
        {
          _logger.LogInformation("Finished notification {NotificationName} in {Duration}ms",
              notificationName, duration.TotalMilliseconds);
        }
      }
      catch (Exception ex)
      {
        if (_env.IsDevelopment())
        {
          _logger.LogError(ex, "Error handling notification {NotificationName} with payload {@Notification}",
              notificationName, notification);
        }
        else
        {
          _logger.LogError("Error handling notification {NotificationName}: {ErrorMessage}",
              notificationName, ex.Message);
        }

        throw;
      }
    }
  }
}
