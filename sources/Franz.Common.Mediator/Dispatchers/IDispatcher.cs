using Franz.Common.Mediator.Messages;

namespace Franz.Common.Mediator.Dispatchers;

public interface IDispatcher
{
  // Overload for commands with a response
  Task<TResponse> Send<TResponse>(
      ICommand<TResponse> command,
      CancellationToken cancellationToken = default);

  // Overload for commands with no response
  Task Send(
      ICommand command,
      CancellationToken cancellationToken = default);

  // Overload for queries
  Task<TResponse> Send<TResponse>(
      IQuery<TResponse> query,
      CancellationToken cancellationToken = default);

  // 🔹 NEW: Overload for notifications (includes DomainEvents, IntegrationEvents, etc.)
  Task Send(
      INotification notification,
      CancellationToken cancellationToken = default);
}
