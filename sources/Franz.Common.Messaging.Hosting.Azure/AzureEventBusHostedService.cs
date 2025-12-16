using Franz.Common.Messaging.AzureEventBus.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Azure.EventBus;

internal sealed class AzureEventBusHostedService : IHostedService
{
  private readonly AzureEventBusProcessor _processor;
  private readonly ILogger<AzureEventBusHostedService> _logger;

  public AzureEventBusHostedService(
      AzureEventBusProcessor processor,
      ILogger<AzureEventBusHostedService> logger)
  {
    _processor = processor;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🚀 Starting Azure Service Bus listener");
    await _processor.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🛑 Stopping Azure Service Bus listener");
    await _processor.StopAsync(cancellationToken);
  }
}
