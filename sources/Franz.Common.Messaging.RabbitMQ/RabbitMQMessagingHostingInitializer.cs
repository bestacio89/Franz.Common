using Franz.Common.Hosting;

namespace Franz.Common.Messaging.RabbitMQ;

public class RabbitMQMessagingHostingInitializer : IHostingInitializer
{
  private readonly IMessagingInitializer? messagingInitializer;

  public RabbitMQMessagingHostingInitializer(IMessagingInitializer? messagingInitializer = null)
  {
    this.messagingInitializer = messagingInitializer;
  }

  public int Order => 2;

  public void Initialize()
  {
    messagingInitializer?.InitializeAsync();
  }
}
