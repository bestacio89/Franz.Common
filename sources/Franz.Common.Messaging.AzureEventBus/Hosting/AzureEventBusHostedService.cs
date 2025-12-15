using Franz.Common.Messaging.AzureEventBus.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.AzureEventBus.Hosting;

internal sealed class AzureEventBusHostedService : IHostedService
{
  private readonly AzureEventBusConsumer _consumer;
  private readonly ILogger<AzureEventBusHostedService> _logger;

  public AzureEventBusHostedService(
      AzureEventBusConsumer consumer,
      ILogger<AzureEventBusHostedService> logger)
  {
    _consumer = consumer;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🚀 Starting Azure Event Bus consumer");
    await _consumer.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🛑 Stopping Azure Event Bus consumer");
    await _consumer.StopAsync(cancellationToken);
  }
}
