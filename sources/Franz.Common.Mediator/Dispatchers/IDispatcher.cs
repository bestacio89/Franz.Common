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

  /// <summary>
  /// Publishes a notification using its concrete runtime type and generated dispatch registration.
  /// Use this overload at deserialization boundaries where the static type is only INotification.
  /// </summary>
  Task PublishNotificationAsync(
    INotification notification,
    CancellationToken cancellationToken = default,
    PublishStrategy strategy = PublishStrategy.Sequential,
    NotificationErrorHandling errorHandling = NotificationErrorHandling.ContinueOnError)
  {
    System.ArgumentNullException.ThrowIfNull(notification);
    throw new System.NotSupportedException(
        "This dispatcher does not support runtime-typed notification dispatch.");
  }

  public Task PublishNotificationAsync<TNotification>(
    TNotification notification,
    CancellationToken cancellationToken = default,
    PublishStrategy strategy = PublishStrategy.Sequential,
    NotificationErrorHandling errorHandling = NotificationErrorHandling.ContinueOnError)
    where TNotification : INotification;

  // NEW: Generic event publishing
  Task PublishEventAsync<TEvent>(
      TEvent @event,
      CancellationToken cancellationToken = default)
      where TEvent : IEvent;
}
