using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation
{


  public class NotificationValidationPipeline<TNotification> : INotificationPipeline<TNotification>
      where TNotification : Messages.INotification
  {
    private readonly IEnumerable<INotificationValidator<TNotification>> _validators;
    private readonly ILogger<NotificationValidationPipeline<TNotification>> _logger;
    private readonly IHostEnvironment _env;

    public NotificationValidationPipeline(
      IEnumerable<INotificationValidator<TNotification>> validators,
      ILogger<NotificationValidationPipeline<TNotification>> logger,
      IHostEnvironment env)
    {
      _validators = validators;
      _logger = logger;
      _env = env;
    }

    public async Task Handle(
        TNotification notification,
        Func<Task> next,
        CancellationToken cancellationToken = default)
    {
      var failures = new List<string>();

      foreach (var validator in _validators)
      {
        var result = await validator.ValidateAsync(notification, cancellationToken);
        if (result != null)
          failures.AddRange(result);
      }

      if (failures.Any())
      {
        var notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;

        if (_env.IsDevelopment())
        {
          // 🔥 Dev: log all validation failures
          _logger.LogWarning("[NotificationValidation] {NotificationName} failed with errors: {@Errors}",
              notificationName, failures);
        }
        else
        {
          // 🟢 Prod: log only count
          _logger.LogWarning("[NotificationValidation] {NotificationName} failed with {ErrorCount} errors",
              notificationName, failures.Count);
        }

        throw new NotificationValidationException(failures);
      }

      if (_env.IsDevelopment())
      {
        var notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;
        _logger.LogInformation("[NotificationValidation] {NotificationName} passed", notificationName);
      }

      await next();
    }
  }

  public class NotificationValidationException : Exception
  {
    public IEnumerable<string> Errors { get; }
    public NotificationValidationException(IEnumerable<string> errors)
        : base("Notification validation failed")
    {
      Errors = errors;
    }
  }
}
