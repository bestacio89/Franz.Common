using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Logging
{
  public class NotificationLoggingPipeline<TNotification> : INotificationPipeline<TNotification>
      where TNotification : Messages.INotification
  {
    private readonly ILogger<NotificationLoggingPipeline<TNotification>> _logger;

    public NotificationLoggingPipeline(ILogger<NotificationLoggingPipeline<TNotification>> logger)
    {
      _logger = logger;
    }

    public async Task Handle(
        TNotification notification,
        Func<Task> next,
        CancellationToken cancellationToken = default)
    {
      var notificationName = typeof(TNotification).Name;
      _logger.LogInformation("Starting notification {NotificationName}", notificationName);

      var start = DateTime.UtcNow;
      try
      {
        await next();
        var duration = DateTime.UtcNow - start;
        _logger.LogInformation("Finished notification {NotificationName} in {Duration}ms",
            notificationName, duration.TotalMilliseconds);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error handling notification {NotificationName}", notificationName);
        throw;
      }
    }
  }
}
