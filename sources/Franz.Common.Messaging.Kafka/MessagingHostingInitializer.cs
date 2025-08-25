#nullable enable
using Franz.Common.Hosting;

namespace Franz.Common.Messaging.Kafka;

public class MessagingHostingInitializer : IHostingInitializer
{
  private readonly IMessagingInitializer? messagingInitializer;

  public MessagingHostingInitializer(IMessagingInitializer? messagingInitializer = null)
  {
    this.messagingInitializer = messagingInitializer;
  }

  public int Order => 2;

  public void Initialize()
  {
    messagingInitializer?.Initialize();
  }
}
