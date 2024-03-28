namespace Franz.Common.Messaging.Contexting;

public class MessageContext : IMessageContext
{
    public MessageContext(Message message)
    {
        Message = message;
    }

    public Message Message { get; }
}
