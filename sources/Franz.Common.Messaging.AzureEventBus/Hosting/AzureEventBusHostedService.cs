using Franz.Common.Messaging.AzureEventBus.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Franz.Common.Messaging.MultiTenancy;

namespace Franz.Common.Messaging.AzureEventBus.Hosting;

public sealed class AzureEventBusHostedService : IHostedService
{
  private readonly AzureEventBusConsumer _consumer;
  private readonly ILogger<AzureEventBusHostedService> _logger;
  private readonly IServiceProvider? _serviceProvider;

  public AzureEventBusHostedService(
      AzureEventBusConsumer consumer,
      ILogger<AzureEventBusHostedService> logger)
      : this(consumer, logger, serviceProvider: null)
  {
  }

  public AzureEventBusHostedService(
      AzureEventBusConsumer consumer,
      ILogger<AzureEventBusHostedService> logger,
      IServiceProvider? serviceProvider)
  {
    _consumer = consumer;
    _logger = logger;
    _serviceProvider = serviceProvider;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (_serviceProvider is not null)
      TenantMessagingStartupValidation.Validate(_serviceProvider);
    _logger.LogInformation("🚀 Starting Azure Event Bus consumer");
    await _consumer.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("🛑 Stopping Azure Event Bus consumer");
    await _consumer.StopAsync(cancellationToken);
  }
}
