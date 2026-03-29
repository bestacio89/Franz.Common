#nullable enable
using Franz.Common.Messaging.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Delegating;

/// <summary>
/// Defines the pipeline handler for processing messages before they are dispatched to the transport.
/// Senior Note: Redefined as Task-based to support non-blocking middleware and I/O operations.
/// </summary>
public interface IMessageHandler
{
  /// <summary>
  /// Processes the message through the internal pipeline.
  /// </summary>
  /// <param name="message">The Franz message envelope.</param>
  /// <param name="ct">The cancellation token to honor graceful shutdowns.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task ProcessAsync(Message message, CancellationToken ct = default);
}