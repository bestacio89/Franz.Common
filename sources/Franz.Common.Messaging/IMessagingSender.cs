using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging;

public interface IMessagingSender
{
  /// <summary>
  /// Sends a transport-level message to the configured broker (e.g., Kafka).
  /// </summary>
  Task SendAsync(Message message, CancellationToken cancellationToken = default);
}
