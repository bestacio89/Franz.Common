using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Messages
{
  public interface INotificationHandler<in TNotification>
      where TNotification : INotification
  {
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
  }
}
