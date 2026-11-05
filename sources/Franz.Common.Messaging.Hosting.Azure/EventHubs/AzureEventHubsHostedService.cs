using Franz.Common.Messaging.AzureEventBus.Consumers;
using Franz.Common.Messaging.AzureEventHubs.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Messaging.MultiTenancy;

namespace Franz.Common.Messaging.Hosting.Azure.EventHubs;

public sealed class AzureEventHubsHostedService : IHostedService
{
  private readonly AzureEventHubsProcessor _processor;
  private readonly ILogger<AzureEventHubsHostedService> _logger;
  private readonly IServiceProvider? _serviceProvider;

  public AzureEventHubsHostedService(
      AzureEventHubsProcessor processor,
      ILogger<AzureEventHubsHostedService> logger)
      : this(processor, logger, serviceProvider: null)
  {
  }

  public AzureEventHubsHostedService(
      AzureEventHubsProcessor processor,
      ILogger<AzureEventHubsHostedService> logger,
      IServiceProvider? serviceProvider)
  {
    _processor = processor;
    _logger = logger;
    _serviceProvider = serviceProvider;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (_serviceProvider is not null)
      TenantMessagingStartupValidation.Validate(_serviceProvider);
    _logger.LogInformation("🚀 Starting Azure Event Hubs listener");
    await _processor.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🛑 Stopping Azure Event Hubs listener");
    await _processor.StopAsync(cancellationToken);
  }
}
