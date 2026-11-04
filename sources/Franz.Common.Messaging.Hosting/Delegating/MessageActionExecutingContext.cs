using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Hosting.Delegating;

public class MessageActionExecutingContext
{
    public MessageActionExecutingContext(Message message, CancellationToken cancellationToken = default)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CancellationToken = cancellationToken;
    }

    public Message Message { get; }
    public CancellationToken CancellationToken { get; }
}
