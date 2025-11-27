#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Marks a message type that starts a new saga instance.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IStartWith<in TMessage>
{
  /// <summary>
  /// Handles the first message in the saga lifecycle.
  /// A new saga instance will be created for this call.
  /// </summary>
  Task<ISagaTransition> HandleAsync(
    TMessage message,
    ISagaContext context,
    CancellationToken cancellationToken = default);
}
