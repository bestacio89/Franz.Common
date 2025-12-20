using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Hosting;

public class MessageEventArgs : EventArgs
{
    public MessageEventArgs(Message message)
    {
        Message = message;
    }

    public Message Message { get; private set; }
}
