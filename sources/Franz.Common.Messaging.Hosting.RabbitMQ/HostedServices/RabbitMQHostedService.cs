using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Hosting.RabbitMQ.Abstractions;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

public sealed class RabbitMQHostedService : BackgroundService
{
  private readonly Listener _listener;
  private readonly IQueueProvisioner? _provisioner;
  private readonly IModelProvider _modelProvider;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly ILogger<RabbitMQHostedService> _logger;

  public RabbitMQHostedService(
    Listener listener,
    IModelProvider modelProvider,
    IAssemblyAccessor assemblyAccessor,
    ILogger<RabbitMQHostedService> logger,
    IQueueProvisioner? provisioner = null) // ✅ OPTIONAL
  {
    _listener = listener;
    _modelProvider = modelProvider;
    _assemblyAccessor = assemblyAccessor;
    _logger = logger;
    _provisioner = provisioner;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("🚀 RabbitMQHostedService starting");

    // ✅ OPT-IN provisioning
    if (_provisioner != null)
    {
      var queueName = QueueNamer.GetQueueName(
        _assemblyAccessor.GetEntryAssembly());

      _logger.LogInformation(
        "Ensuring RabbitMQ queue {Queue}", queueName);

      await _provisioner.EnsureQueueExistsAsync(
        _modelProvider,
        queueName,
        stoppingToken);
    }

    // ✅ Safe to start consuming now
    await _listener.Listen(stoppingToken);

    _logger.LogInformation("🛑 RabbitMQHostedService stopping");
  }
}
