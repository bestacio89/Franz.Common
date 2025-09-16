using Franz.Common.Mediator.Messages;

namespace Franz.Common.Mediator.Pipelines.Core
{
  public interface INotificationPipeline<TNotification>
      where TNotification : INotification
  {
    Task Handle(
        TNotification notification,
        Func<Task> next,
        CancellationToken cancellationToken);
  }
}
