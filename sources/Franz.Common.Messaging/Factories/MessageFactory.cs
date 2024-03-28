using Franz.Common.Errors;
using Franz.Common.Messaging.Properties;

namespace Franz.Common.Messaging.Factories;

public class MessageFactory : IMessageFactory
{
    private readonly IEnumerable<IMessageBuilderStrategy> messageBuilderStrategies;

    public MessageFactory(IEnumerable<IMessageBuilderStrategy> messageBuilderStrategies)
    {
        this.messageBuilderStrategies = messageBuilderStrategies;
    }

    public Message Build(object value)
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        IMessageBuilderStrategy? messageBuilderStrategy = messageBuilderStrategies.SingleOrDefault(messageBuilderStrategy => messageBuilderStrategy.CanBuild(value));
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        if (messageBuilderStrategy == null)
        {
            throw new TechnicalException(string.Format(Resources.MessagingBuilderStrategyNotFoundException, value.GetType()));
        }

        var result = messageBuilderStrategy.Build(value);

        return result;
    }
}
