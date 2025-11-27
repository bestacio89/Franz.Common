#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Handles a message as part of an existing saga instance.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IHandle<in TMessage>
{
  /// <summary>
  /// Handles the message for an existing saga instance.
  /// The orchestrator will ensure the correct saga instance is loaded.
  /// </summary>
  Task<ISagaTransition> HandleAsync(
    TMessage message,
    ISagaContext context,
    CancellationToken cancellationToken = default);
}
