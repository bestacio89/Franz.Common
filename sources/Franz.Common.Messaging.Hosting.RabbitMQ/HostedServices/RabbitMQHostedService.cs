using Franz.Common.Messaging.Hosting.RabbitMQ.Abstractions;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

/// <summary>
/// Keeps the RabbitMQ consumer task attached to the generic host lifecycle.
/// </summary>
public sealed class RabbitMQHostedService : BackgroundService
{
  private readonly RabbitMQListener _listener;
  private readonly IQueueProvisioner? _provisioner;
  private readonly IModelProvider _modelProvider;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly ILogger<RabbitMQHostedService> _logger;

  public RabbitMQHostedService(
      RabbitMQListener listener,
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      ILogger<RabbitMQHostedService> logger,
      IQueueProvisioner? provisioner = null)
  {
    _listener = listener;
    _modelProvider = modelProvider;
    _assemblyAccessor = assemblyAccessor;
    _logger = logger;
    _provisioner = provisioner;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("RabbitMQ hosted consumer starting");

    if (_provisioner is not null)
    {
      var assembly = _assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null.");
      var queueName = QueueNamer.GetQueueName(assembly);

      _logger.LogInformation("Ensuring RabbitMQ queue {Queue}", queueName);
      await _provisioner.EnsureQueueExistsAsync(_modelProvider, queueName, stoppingToken)
          .ConfigureAwait(false);
    }

    await _listener.Listen(stoppingToken).ConfigureAwait(false);

    // Listen returns once the broker consumer is ready. Keep ExecuteAsync alive so
    // the host observes unexpected consumer termination and faults.
    await _listener.Completion.WaitAsync(stoppingToken).ConfigureAwait(false);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    try
    {
      await _listener.StopListenAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      // Always complete the BackgroundService shutdown path, even if the consumer
      // task previously faulted.
      await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
  }
}