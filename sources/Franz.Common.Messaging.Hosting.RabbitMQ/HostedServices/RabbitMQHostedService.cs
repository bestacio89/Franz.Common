using Franz.Common.Messaging.Hosting.RabbitMQ.Abstractions;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

/// <summary>
/// Keeps the RabbitMQ consumer task attached to the generic host lifecycle.
/// </summary>
public sealed class RabbitMQHostedService : BackgroundService
{
  private readonly RabbitMQListener _listener;
  private readonly IQueueProvisioner? _provisioner;
  private readonly IModelProvider _modelProvider;
  private readonly ILogger<RabbitMQHostedService> _logger;
  private readonly string _queueName;

  /// <summary>
  /// Creates the hosted service using the assembly-derived queue name.
  /// </summary>
  public RabbitMQHostedService(
      RabbitMQListener listener,
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      ILogger<RabbitMQHostedService> logger,
      IQueueProvisioner? provisioner = null)
      : this(listener, modelProvider, assemblyAccessor, logger, provisioner, options: null)
  {
  }

  /// <summary>
  /// Creates the hosted service using the configured queue name when available.
  /// </summary>
  public RabbitMQHostedService(
      RabbitMQListener listener,
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      ILogger<RabbitMQHostedService> logger,
      IQueueProvisioner? provisioner,
      IOptions<RabbitMQMessagingOptions>? options)
  {
    _listener = listener;
    _modelProvider = modelProvider;
    _logger = logger;
    _provisioner = provisioner;

    var configuredQueueName = options?.Value.QueueName;
    _queueName = !string.IsNullOrWhiteSpace(configuredQueueName)
        ? configuredQueueName
        : RabbitMQQueueNamer.GetQueueName(
            assemblyAccessor.GetEntryAssembly()
            ?? throw new InvalidOperationException("Entry assembly cannot be null."));
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("RabbitMQ hosted consumer starting");

    if (_provisioner is not null)
    {
      _logger.LogInformation("Ensuring RabbitMQ queue {Queue}", _queueName);
      await _provisioner.EnsureQueueExistsAsync(_modelProvider, _queueName, stoppingToken)
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