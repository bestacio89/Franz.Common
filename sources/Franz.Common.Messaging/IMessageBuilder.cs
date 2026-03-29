#nullable enable
using Franz.Common.DependencyInjection;
using Franz.Common.Messaging.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging;

/// <summary>
/// Defines a strategy for enriching message metadata or content before transport.
/// Senior Note: Moving to Task-based BuildAsync to support high-performance I/O enrichment.
/// </summary>
public interface IMessageBuilder : IScopedDependency
{
  /// <summary>
  /// Determines if this builder should process the given message.
  /// Senior Note: Remains synchronous as it's a predicate check on local state.
  /// </summary>
  bool CanBuild(Message message);

  /// <summary>
  /// Asynchronously builds or enriches the message.
  /// </summary>
  /// <param name="message">The Franz message envelope.</param>
  /// <param name="ct">The cancellation token for the host's lifetime.</param>
  Task BuildAsync(Message message, CancellationToken ct = default);
}