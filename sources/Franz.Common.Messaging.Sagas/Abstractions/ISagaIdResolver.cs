#nullable enable

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Resolves the identity of the saga instance addressed by an incoming message.
/// This identity is the saga repository key and is distinct from message correlation
/// and causation metadata.
/// </summary>
/// <typeparam name="TMessage">The incoming message type.</typeparam>
public interface ISagaIdResolver<in TMessage>
{
  /// <summary>
  /// Resolves the non-empty saga instance identifier addressed by the message.
  /// </summary>
  /// <param name="message">The incoming message.</param>
  /// <returns>The saga instance identifier.</returns>
  Guid ResolveSagaId(TMessage message);
}
