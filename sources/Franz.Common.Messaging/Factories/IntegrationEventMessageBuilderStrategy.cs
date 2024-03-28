using Franz.Common.Business.Events;

using Newtonsoft.Json;

namespace Franz.Common.Messaging.Factories;

public class IntegrationEventMessageBuilderStrategy : IMessageBuilderStrategy
{
    public bool CanBuild(object value)
    {
        var result = value is IIntegrationEvent;

        return result;
    }

    public Message Build(object value)
    {
        var messagingEvent = (IIntegrationEvent)value;

        var messageBody = JsonConvert.SerializeObject(messagingEvent);
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        Message? result = new(messageBody);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        var headerName = HeaderNamer.GetEventClassName(value.GetType());
        result.Headers.Add(MessagingConstants.ClassName, headerName);

        return result;
    }
}
