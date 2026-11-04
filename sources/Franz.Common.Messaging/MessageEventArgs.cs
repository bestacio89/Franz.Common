using Franz.Common.Messaging.Messages;
using System.Threading;

namespace Franz.Common.Messaging.Hosting;

public class MessageEventArgs : EventArgs
{
    public MessageEventArgs(Message message)
        : this(message, CancellationToken.None)
    {
    }

    public MessageEventArgs(Message message, CancellationToken cancellationToken)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CancellationToken = cancellationToken;
    }

    public Message Message { get; private set; }

    /// <summary>Cancellation token for cooperative message-handler cancellation.</summary>
    public CancellationToken CancellationToken { get; }
}
