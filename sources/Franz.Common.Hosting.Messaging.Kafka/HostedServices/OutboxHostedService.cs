using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Listeners;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;

public class OutboxHostedService : BackgroundService
{
  private readonly OutboxMessageListener _listener;
  private readonly ILogger<OutboxHostedService> _logger;

  public OutboxHostedService(OutboxMessageListener listener, ILogger<OutboxHostedService> logger)
  {
    _listener = listener;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("🚀 OutboxHostedService starting");
    await _listener.Listen(stoppingToken);
    _logger.LogInformation("🛑 OutboxHostedService stopping");
  }
}
