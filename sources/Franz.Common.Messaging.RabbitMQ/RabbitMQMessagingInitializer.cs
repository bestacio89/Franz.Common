#nullable enable
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingInitializer : IMessagingInitializer
{
  private bool _isInitialized;
  private readonly SemaphoreSlim _initLock = new(1, 1);

  private readonly IModelProvider _modelProvider;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly RabbitMQMessagingOptions _options;

  private readonly string _exchangeName;
  private readonly string _queueName;
  private readonly string _deadLetterQueueName;
  private readonly string _deadLetterExchangeName;

  private static Type[]? _cachedIntegrationEvents;

  public RabbitMQMessagingInitializer(
      IModelProvider modelProvider,
      IAssemblyAccessor assemblyAccessor,
      IOptions<RabbitMQMessagingOptions> options)
  {
    _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
    _assemblyAccessor = assemblyAccessor ?? throw new ArgumentNullException(nameof(assemblyAccessor));
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    var assembly = _assemblyAccessor.GetEntryAssembly()
        ?? throw new InvalidOperationException("Entry assembly cannot be null.");

    _exchangeName = _options.ExchangeName ?? ExchangeNamer.GetEventExchangeName(assembly);
    _queueName = _options.QueueName ?? QueueNamer.GetQueueName(assembly);
    _deadLetterQueueName = _options.DeadLetterQueueName ?? QueueNamer.GetDeadLetterQueueName(assembly);
    _deadLetterExchangeName = _options.DeadLetterExchangeName ?? ExchangeNamer.GetDeadLetterExchangeName(assembly);
  }

  public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
  {
    if (_isInitialized) return;

    await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (_isInitialized) return;

      var channel = await _modelProvider.GetChannelAsync(cancellationToken).ConfigureAwait(false);

      await InitializeExchangeAsync(channel, _exchangeName, ExchangeType.Headers, true).ConfigureAwait(false);
      await InitializeQueueAsync(channel, _queueName, _exchangeName, _deadLetterExchangeName, _deadLetterQueueName, true).ConfigureAwait(false);
      await InitializeDeadLetterQueueAsync(channel, _deadLetterQueueName, _deadLetterExchangeName).ConfigureAwait(false);
      await InitializeExchangesForSubscriptionsAsync(channel, cancellationToken).ConfigureAwait(false);

      _isInitialized = true;
    }
    finally
    {
      _initLock.Release();
    }
  }

  private static async ValueTask InitializeExchangeAsync(
      IChannel channel,
      string exchangeName,
      string type,
      bool durable)
  {
    await channel.ExchangeDeclareAsync(exchangeName, type, durable: durable, autoDelete: false)
        .ConfigureAwait(false);
  }

  private async ValueTask InitializeQueueAsync(
      IChannel channel,
      string queueName,
      string exchangeName,
      string deadLetterExchange,
      string deadLetterQueue,
      bool durable)
  {
    var args = new Dictionary<string, object?>
    {
      ["x-queue-type"] = "quorum",
      ["x-dead-letter-exchange"] = deadLetterExchange,
      ["x-dead-letter-routing-key"] = deadLetterQueue
    };

    if (_options.RequestedHeartbeatSeconds.HasValue)
    {
      args["x-heartbeat"] = _options.RequestedHeartbeatSeconds.Value;
    }

    await channel.QueueDeclareAsync(
        queueName,
        durable,
        exclusive: false,
        autoDelete: false,
        arguments: args).ConfigureAwait(false);
  }

  private async ValueTask InitializeDeadLetterQueueAsync(
      IChannel channel,
      string deadLetterQueueName,
      string deadLetterExchangeName)
  {
    await channel.ExchangeDeclareAsync(
        deadLetterExchangeName,
        ExchangeType.Direct,
        durable: true,
        autoDelete: false).ConfigureAwait(false);

    await channel.QueueDeclareAsync(
        deadLetterQueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: new Dictionary<string, object?>
        {
          ["x-queue-type"] = "quorum"
        }).ConfigureAwait(false);

    await channel.QueueBindAsync(
        deadLetterQueueName,
        deadLetterExchangeName,
        routingKey: deadLetterQueueName).ConfigureAwait(false);
  }

  private async ValueTask InitializeExchangesForSubscriptionsAsync(
      IChannel channel,
      CancellationToken cancellationToken)
  {
    var entryAssembly = _assemblyAccessor.GetEntryAssembly()
        ?? throw new InvalidOperationException("Entry assembly cannot be null.");

    var companyName = entryAssembly.Name?.Split('.').FirstOrDefault() ?? string.Empty;

    if (_cachedIntegrationEvents is null)
    {
      _cachedIntegrationEvents = AppDomain.CurrentDomain
          .GetAssemblies()
          .Where(a => !a.IsDynamic &&
                      a.FullName?.StartsWith(companyName, StringComparison.OrdinalIgnoreCase) == true)
          .SelectMany(a => a.ExportedTypes)
          .Where(t => t.GetInterfaces().Any(ifc =>
              ifc.IsGenericType &&
              ifc.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
          .SelectMany(t => t.GetInterfaces())
          .Where(ifc => ifc.IsGenericType &&
                        ifc.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
          .Select(ifc => ifc.GetGenericArguments()[0])
          .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
          .Distinct()
          .ToArray();
    }

    foreach (var eventType in _cachedIntegrationEvents)
    {
      await InitializeExchangeForSubscriptionAsync(channel, eventType)
          .ConfigureAwait(false);
    }
  }

  private static async ValueTask InitializeExchangeForSubscriptionAsync(
      IChannel channel,
      Type integrationEventType)
  {
    var sourceExchange = ExchangeNamer.GetEventExchangeName(integrationEventType.Assembly);
    var eventName = HeaderNamer.GetEventClassName(integrationEventType);

    await channel.QueueBindAsync(
        queue: QueueNamer.GetQueueName(integrationEventType.Assembly),
        exchange: sourceExchange,
        routingKey: string.Empty,
        arguments: new Dictionary<string, object?>
        {
          [MessagingConstants.ClassName] = eventName,
          ["x-match"] = "all"
        }).ConfigureAwait(false);
  }
}