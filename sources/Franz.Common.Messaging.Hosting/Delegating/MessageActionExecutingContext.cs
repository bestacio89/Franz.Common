using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Hosting.Delegating;

public class MessageActionExecutingContext
{
    public MessageActionExecutingContext(Message message)
    {
        Message = message;
    }

    public Message Message { get; }
}
