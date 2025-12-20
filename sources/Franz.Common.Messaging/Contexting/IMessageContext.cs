using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Contexting;

public interface IMessageContext
{
    Message Message { get; }
}
