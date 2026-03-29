using Franz.Common.Messaging.Messages;
using System.Threading.Tasks;

namespace Franz.Common.Messaging;

public interface IMessagingSender : IAsyncDisposable, IDisposable
{
  /// <summary>
  /// Sends a transport-level message to the configured broker (e.g., Kafka).
  /// </summary>
  ValueTask SendAsync(Message message, CancellationToken cancellationToken = default);
}
