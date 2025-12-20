using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Hosting.Delegating;

public class MessageActionExecutedContext
{
    public MessageActionExecutedContext(Message message)
    {
        Message = message;
    }

    public virtual Message Message { get; }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public virtual Exception? Exception { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}
