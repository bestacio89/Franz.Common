using Franz.Common.Messaging.Hosting.Listeners;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting.Kafka.HostedServices;
public class KafkaHostedService : BackgroundService
{
  private readonly KafkaMessageListener _listener;
  private readonly ILogger<KafkaHostedService> _logger;

  public KafkaHostedService(KafkaMessageListener listener, ILogger<KafkaHostedService> logger)
  {
    _listener = listener;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("🚀 KafkaHostedService starting");
    await _listener.Listen(stoppingToken);
    _logger.LogInformation("🛑 KafkaHostedService stopping");
  }
}
