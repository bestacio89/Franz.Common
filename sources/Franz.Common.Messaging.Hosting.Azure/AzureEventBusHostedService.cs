using Franz.Common.Messaging.AzureEventBus.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Messaging.MultiTenancy;

namespace Franz.Common.Messaging.Hosting.Azure.EventBus;

internal sealed class AzureEventBusHostedService : IHostedService
{
  private readonly AzureEventBusProcessor _processor;
  private readonly ILogger<AzureEventBusHostedService> _logger;
  private readonly IServiceProvider _serviceProvider;

  public AzureEventBusHostedService(
      AzureEventBusProcessor processor,
      ILogger<AzureEventBusHostedService> logger,
      IServiceProvider serviceProvider)
  {
    _processor = processor;
    _logger = logger;
    _serviceProvider = serviceProvider;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    TenantMessagingStartupValidation.Validate(_serviceProvider);
    _logger.LogInformation("🚀 Starting Azure Service Bus listener");
    await _processor.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🛑 Stopping Azure Service Bus listener");
    await _processor.StopAsync(cancellationToken);
  }
}
