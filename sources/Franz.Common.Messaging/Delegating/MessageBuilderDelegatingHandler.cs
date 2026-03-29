#nullable enable
using Franz.Common.Messaging.Messages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Delegating;

/// <summary>
/// Orchestrates the enrichment of messages using a collection of builders.
/// Senior Note: Refactored to Task-based execution to support asynchronous enrichment.
/// </summary>
public sealed class MessageBuilderDelegatingHandler : IMessageHandler
{
  private readonly IEnumerable<IMessageBuilder> _messageBuilders;

  public MessageBuilderDelegatingHandler(IEnumerable<IMessageBuilder> messageBuilders)
  {
    _messageBuilders = messageBuilders ?? throw new ArgumentNullException(nameof(messageBuilders));
  }

  public async Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    // Filter and execute builders sequentially to ensure predictable message state
    foreach (var builder in _messageBuilders)
    {
      if (builder.CanBuild(message))
      {
        // Senior Note: Awaiting each builder allows for async metadata 
        // lookups without starving the thread pool.
        await builder.BuildAsync(message, ct);
      }
    }
  }
}