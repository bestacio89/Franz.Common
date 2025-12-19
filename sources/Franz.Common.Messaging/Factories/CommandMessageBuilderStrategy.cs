using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Franz.Common.Messaging.Factories;

public class CommandMessageBuilderStrategy : IMessageBuilderStrategy
{
    public bool CanBuild(object value)
    {
        var result = value is ICommand;

        return result;
    }

    public Message Build(object value)
    {
        var messagingCommand = (ICommand)value;

        var messageBody = JsonConvert.SerializeObject(messagingCommand);
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        Message? result = new(messageBody);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        var headerName = HeaderNamer.GetEventClassName(value.GetType());
    result.Headers.Add(
      MessagingConstants.ClassName,
      new StringValues(headerName)
      );


    return result;
    }
}
