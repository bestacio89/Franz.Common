using Franz.Common.Business.Commands;

using Newtonsoft.Json;

namespace Franz.Common.Messaging.Factories;

public class CommandMessageBuilderStrategy : IMessageBuilderStrategy
{
    public bool CanBuild(object value)
    {
        var result = value is ICommandBaseRequest;

        return result;
    }

    public Message Build(object value)
    {
        var messagingCommand = (ICommandBaseRequest)value;

        var messageBody = JsonConvert.SerializeObject(messagingCommand);
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        Message? result = new(messageBody);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        var headerName = HeaderNamer.GetEventClassName(value.GetType());
        result.Headers.Add(MessagingConstants.ClassName, headerName);

        return result;
    }
}
