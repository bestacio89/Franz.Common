#nullable enable
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

  private static readonly ConcurrentDictionary<Assembly, Type[]> CachedIntegrationEvents = new();

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

    _exchangeName = _options.ExchangeName ?? RabbitMQExchangeNamer.GetEventExchangeName(assembly);
    _queueName = _options.QueueName ?? RabbitMQQueueNamer.GetQueueName(assembly);
    _deadLetterQueueName = _options.DeadLetterQueueName ?? RabbitMQQueueNamer.GetDeadLetterQueueName(assembly);
    _deadLetterExchangeName = _options.DeadLetterExchangeName ?? RabbitMQExchangeNamer.GetDeadLetterExchangeName(assembly);
  }

  public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
  {
    if (_isInitialized) return;
    if (_options.ProvisioningMode == ProvisioningMode.Off)
    {
      _isInitialized = true;
      return;
    }

    await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (_isInitialized) return;

      var channel = await _modelProvider.GetChannelAsync(cancellationToken).ConfigureAwait(false);

      if (_options.ProvisioningMode == ProvisioningMode.Verify)
      {
        await VerifyTopologyAsync(channel, cancellationToken).ConfigureAwait(false);
      }
      else
      {
        await InitializeExchangeAsync(channel, _exchangeName, ExchangeType.Headers, true).ConfigureAwait(false);
        await InitializeQueueAsync(channel, _queueName, _exchangeName, _deadLetterExchangeName, _deadLetterQueueName, _options.QueueDurable).ConfigureAwait(false);
        await InitializeDeadLetterQueueAsync(channel, _deadLetterQueueName, _deadLetterExchangeName).ConfigureAwait(false);
        if (string.Equals(_options.ReplayStrategy, "ProgressiveFourStep", StringComparison.OrdinalIgnoreCase))
          await InitializeReplayTopologyAsync(channel).ConfigureAwait(false);
        await InitializeExchangesForSubscriptionsAsync(channel, cancellationToken).ConfigureAwait(false);
      }

      _isInitialized = true;
    }
    finally
    {
      _initLock.Release();
    }
  }

  private async ValueTask VerifyTopologyAsync(IChannel channel, CancellationToken cancellationToken)
  {
    await channel.ExchangeDeclarePassiveAsync(_exchangeName, cancellationToken).ConfigureAwait(false);
    await channel.QueueDeclarePassiveAsync(_queueName, cancellationToken).ConfigureAwait(false);
    await channel.ExchangeDeclarePassiveAsync(_deadLetterExchangeName, cancellationToken).ConfigureAwait(false);
    await channel.QueueDeclarePassiveAsync(_deadLetterQueueName, cancellationToken).ConfigureAwait(false);

    if (string.Equals(_options.ReplayStrategy, "ProgressiveFourStep", StringComparison.OrdinalIgnoreCase))
    {
      var replayExchange = RabbitMQExchangeNamer.GetReplayExchangeName(
          _assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null."));
      await channel.ExchangeDeclarePassiveAsync(replayExchange, cancellationToken).ConfigureAwait(false);
      for (var retryNumber = 1; retryNumber <= 3; retryNumber++)
      {
        var retryQueue = $"{_queueName}-retry-{retryNumber}";
        await channel.QueueDeclarePassiveAsync(retryQueue, cancellationToken).ConfigureAwait(false);
      }
    }
  }

  private Dictionary<string, object?> CreateQueueArguments()
  {
    var arguments = new Dictionary<string, object?>
    {
      ["x-queue-type"] = GetQueueTypeName()
    };

    if (_options.MessageTtlMilliseconds is { } ttl)
      arguments["x-message-ttl"] = ttl;
    if (_options.MaxLength is { } maxLength)
      arguments["x-max-length"] = maxLength;
    if (_options.MaxLengthBytes is { } maxLengthBytes)
      arguments["x-max-length-bytes"] = maxLengthBytes;
    if (_options.DeliveryLimit is { } deliveryLimit && _options.QueueType == RabbitMQQueueType.Quorum)
      arguments["x-delivery-limit"] = deliveryLimit;

    return arguments;
  }

  private string GetQueueTypeName()
      => _options.QueueType == RabbitMQQueueType.Quorum ? "quorum" : "classic";

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
    var args = CreateQueueArguments();
    args["x-dead-letter-exchange"] = deadLetterExchange;
    args["x-dead-letter-routing-key"] = deadLetterQueue;

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

  private async ValueTask InitializeReplayTopologyAsync(IChannel channel)
  {
    var replayExchange = RabbitMQExchangeNamer.GetReplayExchangeName(_assemblyAccessor.GetEntryAssembly()
        ?? throw new InvalidOperationException("Entry assembly cannot be null."));
    await channel.ExchangeDeclareAsync(replayExchange, ExchangeType.Direct, durable: true, autoDelete: false).ConfigureAwait(false);
    for (var retryNumber = 1; retryNumber <= 3; retryNumber++)
    {
      var routingKey = ProgressiveFourStepReplayStrategy.GetRetryRoutingKey(_queueName, retryNumber);
      var retryQueue = $"{_queueName}-retry-{retryNumber}";
      var arguments = new Dictionary<string, object?>
      {
        ["x-queue-type"] = GetQueueTypeName(),
        ["x-message-ttl"] = ProgressiveFourStepReplayStrategy.GetRetryDelayMilliseconds(retryNumber),
        ["x-dead-letter-exchange"] = string.Empty,
        ["x-dead-letter-routing-key"] = _queueName
      };
      await channel.QueueDeclareAsync(retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: arguments).ConfigureAwait(false);
      await channel.QueueBindAsync(retryQueue, replayExchange, routingKey).ConfigureAwait(false);
    }
  }

  private async ValueTask InitializeExchangesForSubscriptionsAsync(
      IChannel channel,
      CancellationToken cancellationToken)
  {
    var entryAssembly = _assemblyAccessor.GetEntryAssembly()
        ?? throw new InvalidOperationException("Entry assembly cannot be null.");

    var companyName = entryAssembly.Name?.Split('.').FirstOrDefault() ?? string.Empty;

    var integrationEvents = CachedIntegrationEvents.GetOrAdd(entryAssembly.Assembly, _ =>
        AppDomain.CurrentDomain
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
            .ToArray());

    foreach (var eventType in integrationEvents)
    {
      await InitializeExchangeForSubscriptionAsync(channel, eventType)
          .ConfigureAwait(false);
    }
  }

  private static async ValueTask InitializeExchangeForSubscriptionAsync(
      IChannel channel,
      Type integrationEventType)
  {
    var sourceExchange = RabbitMQExchangeNamer.GetEventExchangeName(integrationEventType.Assembly);
    var eventName = HeaderNamer.GetEventClassName(integrationEventType);

    await channel.QueueBindAsync(
        queue: RabbitMQQueueNamer.GetQueueName(integrationEventType.Assembly),
        exchange: sourceExchange,
        routingKey: string.Empty,
        arguments: new Dictionary<string, object?>
        {
          [MessagingConstants.ClassName] = eventName,
          ["x-match"] = "all"
        }).ConfigureAwait(false);
  }
}