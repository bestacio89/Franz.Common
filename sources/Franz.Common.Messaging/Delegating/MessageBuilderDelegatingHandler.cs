using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Delegating;

public class MessageBuilderDelegatingHandler : IMessageHandler
{
  private readonly IEnumerable<IMessageBuilder> messageBuilders;

  public MessageBuilderDelegatingHandler(IEnumerable<IMessageBuilder> messageBuilders)
  {
    this.messageBuilders = messageBuilders;
  }

  public void Process(Message message)
  {
    messageBuilders
      .Where(messageBuilder => messageBuilder.CanBuild(message))
      .ToList()
      .ForEach(messageBuilder => messageBuilder.Build(message));
  }
}
