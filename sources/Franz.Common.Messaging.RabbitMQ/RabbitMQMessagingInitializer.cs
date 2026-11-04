#nullable enable
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Replay;
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
  private int _isInitialized;
  private readonly SemaphoreSlim _initLock = new(1, 1);

  private readonly IModelProvider _modelProvider;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly RabbitMQMessagingOptions _options;

  private readonly string _exchangeName;
  private readonly string _queueName;
  private readonly string _deadLetterQueueName;
  private readonly string _deadLetterExchangeName;

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
    if (Volatile.Read(ref _isInitialized) == 1) return;
    if (_options.ProvisioningMode == ProvisioningMode.Off)
    {
      Volatile.Write(ref _isInitialized, 1);
      return;
    }

    await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (Volatile.Read(ref _isInitialized) == 1) return;

      var channel = await _modelProvider.GetChannelAsync(cancellationToken).ConfigureAwait(false);

      if (_options.ProvisioningMode == ProvisioningMode.Verify)
      {
        await VerifyTopologyAsync(channel, cancellationToken).ConfigureAwait(false);
      }
      else
      {
        await InitializeExchangeAsync(channel, _exchangeName, ExchangeType.Headers, true, cancellationToken).ConfigureAwait(false);
        await InitializeQueueAsync(channel, _queueName, _exchangeName, _deadLetterExchangeName, _deadLetterQueueName, _options.QueueDurable, cancellationToken).ConfigureAwait(false);
        await InitializeDeadLetterQueueAsync(channel, _deadLetterQueueName, _deadLetterExchangeName, cancellationToken).ConfigureAwait(false);
        if (string.Equals(_options.ReplayStrategy, "ProgressiveFourStep", StringComparison.OrdinalIgnoreCase))
          await InitializeReplayTopologyAsync(channel, cancellationToken).ConfigureAwait(false);
        await InitializeExchangesForSubscriptionsAsync(channel, cancellationToken).ConfigureAwait(false);
      }

      Volatile.Write(ref _isInitialized, 1);
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
      var retryAttempts = _options.FailurePolicy?.MaxRetryAttempts ?? 3;
      for (var retryNumber = 1; retryNumber <= retryAttempts; retryNumber++)
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
      bool durable,
      CancellationToken cancellationToken)
  {
    await channel.ExchangeDeclareAsync(exchangeName, type, durable: durable, autoDelete: false, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
  }

  private async ValueTask InitializeQueueAsync(
      IChannel channel,
      string queueName,
      string exchangeName,
      string deadLetterExchange,
      string deadLetterQueue,
      bool durable,
      CancellationToken cancellationToken)
  {
    var args = CreateQueueArguments();
    args["x-dead-letter-exchange"] = deadLetterExchange;
    args["x-dead-letter-routing-key"] = deadLetterQueue;

    await channel.QueueDeclareAsync(
        queueName,
        durable,
        exclusive: false,
        autoDelete: false,
        arguments: args,
        cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  private async ValueTask InitializeDeadLetterQueueAsync(
      IChannel channel,
      string deadLetterQueueName,
      string deadLetterExchangeName,
      CancellationToken cancellationToken)
  {
    await channel.ExchangeDeclareAsync(
        deadLetterExchangeName,
        ExchangeType.Direct,
        durable: true,
        autoDelete: false,
        cancellationToken: cancellationToken).ConfigureAwait(false);

    await channel.QueueDeclareAsync(
        deadLetterQueueName,
        durable: _options.QueueDurable,
        exclusive: false,
        autoDelete: false,
        arguments: new Dictionary<string, object?>
        {
          ["x-queue-type"] = GetQueueTypeName()
        },
        cancellationToken: cancellationToken).ConfigureAwait(false);

    await channel.QueueBindAsync(
        deadLetterQueueName,
        deadLetterExchangeName,
        routingKey: deadLetterQueueName,
        cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  private async ValueTask InitializeReplayTopologyAsync(IChannel channel, CancellationToken cancellationToken)
  {
    var replayExchange = RabbitMQExchangeNamer.GetReplayExchangeName(_assemblyAccessor.GetEntryAssembly()
        ?? throw new InvalidOperationException("Entry assembly cannot be null."));
    await channel.ExchangeDeclareAsync(replayExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
    for (var retryNumber = 1; retryNumber <= 3; retryNumber++)
    {
      var routingKey = ProgressiveFourStepReplayStrategy.GetRetryRoutingKey(_queueName, retryNumber);
      var retryQueue = $"{_queueName}-retry-{retryNumber}";
      var arguments = new Dictionary<string, object?>
      {
        ["x-queue-type"] = GetQueueTypeName(),
        ["x-message-ttl"] = _options.FailurePolicy is { } policy
            ? checked((int)Math.Ceiling(policy.RetryBackoff.TotalMilliseconds))
            : ProgressiveFourStepReplayStrategy.GetRetryDelayMilliseconds(retryNumber),
        ["x-dead-letter-exchange"] = string.Empty,
        ["x-dead-letter-routing-key"] = _queueName
      };
      await channel.QueueDeclareAsync(retryQueue, durable: _options.QueueDurable, exclusive: false, autoDelete: false, arguments: arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
      await channel.QueueBindAsync(retryQueue, replayExchange, routingKey, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
  }

  private async ValueTask InitializeExchangesForSubscriptionsAsync(
      IChannel channel,
      CancellationToken cancellationToken)
  {
    var integrationEvents = Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll()
        .SelectMany(static manifest => manifest.Subscriptions)
        .Distinct()
        .ToArray();

    foreach (var eventType in integrationEvents)
    {
      await InitializeExchangeForSubscriptionAsync(channel, eventType, cancellationToken)
          .ConfigureAwait(false);
    }
  }

  private async ValueTask InitializeExchangeForSubscriptionAsync(
      IChannel channel,
      Type integrationEventType,
      CancellationToken cancellationToken)
  {
    var sourceExchange = RabbitMQExchangeNamer.GetEventExchangeName(integrationEventType.Assembly);
    var eventName = HeaderNamer.GetEventClassName(integrationEventType);

    await channel.QueueBindAsync(
        queue: _queueName,
        exchange: sourceExchange,
        routingKey: string.Empty,
        arguments: new Dictionary<string, object?>
        {
          [MessagingConstants.ClassName] = eventName,
          ["x-match"] = "all"
        },
        cancellationToken: cancellationToken).ConfigureAwait(false);
  }
}