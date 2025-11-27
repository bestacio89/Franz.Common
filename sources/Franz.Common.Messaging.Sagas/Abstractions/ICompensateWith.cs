#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Marks a message as a compensating action for a saga.
/// Usually used when the saga needs to roll back previous work.
/// </summary>
/// <typeparam name="TMessage">The compensation message type.</typeparam>
public interface ICompensateWith<in TMessage>
{
  /// <summary>
  /// Handles a compensating message for the saga instance.
  /// </summary>
  Task<ISagaTransition> HandleAsync(
    TMessage message,
    ISagaContext context,
    CancellationToken cancellationToken = default);
}
