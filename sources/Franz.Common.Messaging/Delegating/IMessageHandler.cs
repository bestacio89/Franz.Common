using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Delegating;

public interface IMessageHandler
{
    void Process(Message message);
}
