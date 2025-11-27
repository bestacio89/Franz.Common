#nullable enable

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Provides a way to derive a saga correlation identifier from a message.
/// This is used by the router to resolve which saga instance should
/// handle a given message.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IMessageCorrelation<in TMessage>
{
  /// <summary>
  /// Derives a correlation identifier from the given message.
  /// </summary>
  /// <param name="message">The incoming message.</param>
  /// <returns>A non-empty correlation identifier.</returns>
  string GetCorrelationId(TMessage message);
}
