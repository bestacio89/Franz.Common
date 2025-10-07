using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
public class RabbitMQHostedService : BackgroundService
{
  private readonly Listener _listener;
  private readonly ILogger<RabbitMQHostedService> _logger;

  public RabbitMQHostedService(Listener listener, ILogger<RabbitMQHostedService> logger)
  {
    _listener = listener;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("🚀 RabbitMQHostedService starting");
    await _listener.Listen(stoppingToken);
    _logger.LogInformation("🛑 RabbitMQHostedService stopping");
  }
}
