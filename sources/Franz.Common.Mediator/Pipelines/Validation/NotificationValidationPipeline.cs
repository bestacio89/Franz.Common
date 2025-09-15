using Franz.Common.Mediator.Pipelines.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation
{
  public interface INotificationValidator<in TNotification>
  {
    Task<IEnumerable<string>> ValidateAsync(
        TNotification notification,
        CancellationToken cancellationToken = default);
  }

  public class NotificationValidationPipeline<TNotification> : INotificationPipeline<TNotification>
      where TNotification : Messages.INotification
  {
    private readonly IEnumerable<INotificationValidator<TNotification>> _validators;

    public NotificationValidationPipeline(IEnumerable<INotificationValidator<TNotification>> validators)
    {
      _validators = validators;
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
        failures.AddRange(result);
      }

      if (failures.Any())
        throw new NotificationValidationException(failures);

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
