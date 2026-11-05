#nullable enable
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Connections;
using RabbitMQ.Client.Exceptions;
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
  private readonly IConnectionProvider _connectionProvider;
  private readonly IAssemblyAccessor _assemblyAccessor;
  private readonly RabbitMQMessagingOptions _options;

  private readonly string _exchangeName;
  private readonly string _queueName;
  private readonly string _deadLetterQueueName;
  private readonly string _deadLetterExchangeName;

  public RabbitMQMessagingInitializer(
      IModelProvider modelProvider,
      IConnectionProvider connectionProvider,
      IAssemblyAccessor assemblyAccessor,
      IOptions<RabbitMQMessagingOptions> options)
  {
    _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
    _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
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
    EnsureMessagingManifestRegistered();
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

      if (_options.ProvisioningMode == ProvisioningMode.Verify)
      {
        await VerifyTopologyAsync(cancellationToken).ConfigureAwait(false);
      }
      else
      {
        var channel = await _modelProvider.GetChannelAsync(cancellationToken).ConfigureAwait(false);
        await InitializeExchangeAsync(channel, _exchangeName, ExchangeType.Headers, true, cancellationToken).ConfigureAwait(false);
        await InitializeDestinationExchangesAsync(channel, cancellationToken).ConfigureAwait(false);
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

  private static void EnsureMessagingManifestRegistered()
  {
    if (Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll().Count == 0)
      throw new InvalidOperationException(
          "No generated messaging manifest is registered. Ensure the Franz mediator source generator is referenced and runs in the application compilation.");
  }

  private async ValueTask VerifyTopologyAsync(CancellationToken cancellationToken)
  {
    var failures = new List<string>();

    await VerifyExchangeAsync(_exchangeName, ExchangeType.Headers, durable: true, failures, cancellationToken)
        .ConfigureAwait(false);

    foreach (var destinationExchange in GetDestinationExchangeNames())
      await VerifyExchangeAsync(destinationExchange, ExchangeType.Headers, durable: true, failures, cancellationToken)
          .ConfigureAwait(false);

    await VerifyQueueAsync(_queueName, _options.QueueDurable, CreateMainQueueArguments(), failures, cancellationToken)
        .ConfigureAwait(false);

    await VerifyExchangeAsync(_deadLetterExchangeName, ExchangeType.Direct, durable: true, failures, cancellationToken)
        .ConfigureAwait(false);
    await VerifyQueueAsync(_deadLetterQueueName, _options.QueueDurable, CreateDeadLetterQueueArguments(), failures, cancellationToken)
        .ConfigureAwait(false);

    if (string.Equals(_options.ReplayStrategy, "ProgressiveFourStep", StringComparison.OrdinalIgnoreCase))
    {
      var replayExchange = RabbitMQExchangeNamer.GetReplayExchangeName(
          _assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null."));
      await VerifyExchangeAsync(replayExchange, ExchangeType.Direct, durable: true, failures, cancellationToken)
          .ConfigureAwait(false);

      for (var retryNumber = 1; retryNumber <= 3; retryNumber++)
      {
        var retryQueue = $"{_queueName}-retry-{retryNumber}";
        await VerifyQueueAsync(retryQueue, _options.QueueDurable,
            CreateRetryQueueArguments(retryNumber), failures, cancellationToken).ConfigureAwait(false);
      }
    }

    if (failures.Count > 0)
      throw new InvalidOperationException(
          "RabbitMQ topology verification failed: " + string.Join("; ", failures));
  }

  private async ValueTask VerifyExchangeAsync(
      string exchangeName,
      string exchangeType,
      bool durable,
      ICollection<string> failures,
      CancellationToken cancellationToken)
  {
    var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
    await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    try
    {
      await channel.ExchangeDeclarePassiveAsync(exchangeName, cancellationToken).ConfigureAwait(false);
      await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable: durable,
          autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (OperationInterruptedException ex)
    {
      failures.Add($"exchange '{exchangeName}' is missing or incompatible ({ex.Message})");
    }
  }

  private async ValueTask VerifyQueueAsync(
      string queueName,
      bool durable,
      IDictionary<string, object?> expectedArguments,
      ICollection<string> failures,
      CancellationToken cancellationToken)
  {
    var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
    await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    try
    {
      await channel.QueueDeclarePassiveAsync(queueName, cancellationToken).ConfigureAwait(false);
      // Redeclaring an existing queue with the expected arguments is idempotent
      // and lets RabbitMQ validate immutable queue type and argument contracts.
      await channel.QueueDeclareAsync(queueName, durable, exclusive: false, autoDelete: false,
          arguments: expectedArguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (OperationInterruptedException ex)
    {
      failures.Add($"queue '{queueName}' is missing or has incompatible type/arguments ({ex.Message})");
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

  private Dictionary<string, object?> CreateMainQueueArguments()
  {
    var arguments = CreateQueueArguments();
    arguments["x-dead-letter-exchange"] = _deadLetterExchangeName;
    arguments["x-dead-letter-routing-key"] = _deadLetterQueueName;
    return arguments;
  }

  private Dictionary<string, object?> CreateDeadLetterQueueArguments()
      => new() { ["x-queue-type"] = GetQueueTypeName() };

  private Dictionary<string, object?> CreateRetryQueueArguments(int retryNumber)
      => new()
      {
        ["x-queue-type"] = GetQueueTypeName(),
        ["x-message-ttl"] = _options.FailurePolicy is { } policy
            ? checked((int)Math.Ceiling(policy.RetryBackoff.TotalMilliseconds))
            : ProgressiveFourStepReplayStrategy.GetRetryDelayMilliseconds(retryNumber),
        ["x-dead-letter-exchange"] = string.Empty,
        ["x-dead-letter-routing-key"] = _queueName
      };

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
    try
    {
      await channel.QueueDeclareAsync(
          queueName,
          durable,
          exclusive: false,
          autoDelete: false,
          arguments: CreateMainQueueArguments(),
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (OperationInterruptedException ex)
    {
      throw new InvalidOperationException(
          $"RabbitMQ queue '{queueName}' could not be redeclared with the configured queue type/arguments. Existing broker topology is incompatible: {ex.Message}",
          ex);
    }
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

    try
    {
      await channel.QueueDeclareAsync(
          deadLetterQueueName,
          durable: _options.QueueDurable,
          exclusive: false,
          autoDelete: false,
          arguments: CreateDeadLetterQueueArguments(),
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (OperationInterruptedException ex)
    {
      throw new InvalidOperationException(
          $"RabbitMQ dead-letter queue '{deadLetterQueueName}' could not be redeclared with the configured queue type/arguments: {ex.Message}",
          ex);
    }

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
    // The policy controls the number of attempts; the topology stays bounded
    // to the three progressive delay stages plus the dead-letter destination.
    for (var retryNumber = 1; retryNumber <= 3; retryNumber++)
    {
      var routingKey = ProgressiveFourStepReplayStrategy.GetRetryRoutingKey(_queueName, retryNumber);
      var retryQueue = $"{_queueName}-retry-{retryNumber}";
      try
      {
        await channel.QueueDeclareAsync(retryQueue, durable: _options.QueueDurable, exclusive: false,
            autoDelete: false, arguments: CreateRetryQueueArguments(retryNumber),
            cancellationToken: cancellationToken).ConfigureAwait(false);
      }
      catch (OperationInterruptedException ex)
      {
        throw new InvalidOperationException(
            $"RabbitMQ retry queue '{retryQueue}' could not be redeclared with the configured queue type/arguments: {ex.Message}",
            ex);
      }
      await channel.QueueBindAsync(retryQueue, replayExchange, routingKey, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
  }

  private IReadOnlyCollection<string> GetDestinationExchangeNames()
  {
    if (!string.IsNullOrWhiteSpace(_options.ExchangeName))
      return Array.Empty<string>();

    return Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll()
        .SelectMany(static manifest => manifest.Destinations)
        .Select(static eventType => RabbitMQExchangeNamer.GetEventExchangeName(eventType.Assembly))
        .Where(exchange => !string.Equals(exchange, _exchangeName, StringComparison.Ordinal))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static exchange => exchange, StringComparer.Ordinal)
        .ToArray();
  }

  private async ValueTask InitializeDestinationExchangesAsync(
      IChannel channel,
      CancellationToken cancellationToken)
  {
    foreach (var exchangeName in GetDestinationExchangeNames())
      await InitializeExchangeAsync(channel, exchangeName, ExchangeType.Headers, true, cancellationToken)
          .ConfigureAwait(false);
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