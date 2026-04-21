#nullable enable
using Franz.Common.Hosting;

namespace Franz.Common.Messaging.Kafka;

public sealed class MessagingHostingInitializer(IMessagingInitializer? messagingInitializer = null)
    : IHostingInitializer
{
  public int Order => 2;

  public async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    if (messagingInitializer is null)
      return;

    await messagingInitializer.InitializeAsync(cancellationToken);
  }
}