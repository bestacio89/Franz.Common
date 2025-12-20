using Franz.Common.Mediator.Messages;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;

namespace Franz.Common.Mediator.Dispatchers;

public interface IDispatcher
{
  // Overload for commands with a response
  Task<TResponse> SendAsync<TResponse>(
      ICommand<TResponse> command,
      CancellationToken cancellationToken = default);

  // Overload for commands with no response
  Task SendAsync(
      ICommand command,
      CancellationToken cancellationToken = default);

  // Overload for queries
  Task<TResponse> SendAsync<TResponse>(
      IQuery<TResponse> query,
      CancellationToken cancellationToken = default);

  // 🔹 NEW: Overload for notifications (includes DomainEvents, IntegrationEvents, etc.)

  public Task PublishNotificationAsync<TNotification>(
    TNotification notification,
    CancellationToken cancellationToken = default,
    PublishStrategy strategy = PublishStrategy.Sequential,
    NotificationErrorHandling errorHandling = NotificationErrorHandling.StopOnFirstFailure)
    where TNotification : INotification;

  // NEW: Generic event publishing
  Task PublishEventAsync<TEvent>(
      TEvent @event,
      CancellationToken cancellationToken = default)
      where TEvent : IEvent;
}
