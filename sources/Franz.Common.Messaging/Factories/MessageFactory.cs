using Franz.Common.Errors;
using Franz.Common.Messaging.Messages;
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
    var strategy = messageBuilderStrategies
        .SingleOrDefault(s => s.CanBuild(value));

    if (strategy == null)
      throw new TechnicalException(
          string.Format(Resources.MessagingBuilderStrategyNotFoundException, value.GetType()));

    return strategy.Build(value);
  }

}
