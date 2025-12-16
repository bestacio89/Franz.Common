using Franz.Common.Messaging.AzureEventBus.Consumers;
using Franz.Common.Messaging.AzureEventHubs.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Azure.EventHubs;

public sealed class AzureEventHubsHostedService : IHostedService
{
  private readonly AzureEventHubsProcessor _processor;
  private readonly ILogger<AzureEventHubsHostedService> _logger;

  public AzureEventHubsHostedService(
      AzureEventHubsProcessor processor,
      ILogger<AzureEventHubsHostedService> logger)
  {
    _processor = processor;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🚀 Starting Azure Event Hubs listener");
    await _processor.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🛑 Stopping Azure Event Hubs listener");
    await _processor.StopAsync(cancellationToken);
  }
}
