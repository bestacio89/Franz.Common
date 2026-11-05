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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingInitializer : IMessagingInitializer
{
  private static readonly HttpClient ManagementApiClient = new(new SocketsHttpHandler
  {
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
  })
  {
    Timeout = TimeSpan.FromSeconds(15)
  };

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
        await InitializeQueueAsync(channel, _queueName, _options.QueueDurable, cancellationToken).ConfigureAwait(false);
        await InitializeDeadLetterQueueAsync(channel, _deadLetterQueueName, _deadLetterExchangeName, cancellationToken).ConfigureAwait(false);
        if (_options.ReplayStrategy == RabbitMQReplayStrategy.ProgressiveFourStep)
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

    if (_options.ReplayStrategy == RabbitMQReplayStrategy.ProgressiveFourStep)
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

    await VerifyBindingsAsync(failures, cancellationToken).ConfigureAwait(false);

    if (failures.Count > 0)
      throw new InvalidOperationException(
          "RabbitMQ topology verification failed: " + string.Join("; ", failures));
  }

  private async ValueTask VerifyBindingsAsync(
      ICollection<string> failures,
      CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(_options.ManagementApiUrl) ||
        !Uri.TryCreate(_options.ManagementApiUrl, UriKind.Absolute, out var managementBaseUri) ||
        (managementBaseUri.Scheme != Uri.UriSchemeHttp && managementBaseUri.Scheme != Uri.UriSchemeHttps))
    {
      failures.Add(
          "binding verification requires a valid Messaging:RabbitMQ:ManagementApiUrl (the RabbitMQ Management HTTP API); AMQP does not expose passive binding inspection");
      return;
    }

    var (username, password) = ResolveManagementCredentials();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
      failures.Add("binding verification requires Management API credentials or reusable AMQP credentials");
      return;
    }

    var virtualHost = ResolveManagementVirtualHost();
    var requestUri = new Uri(
        managementBaseUri,
        $"/api/bindings/{Uri.EscapeDataString(virtualHost)}");

    try
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
      var credentialBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
      request.Headers.Authorization = new AuthenticationHeaderValue(
          "Basic", Convert.ToBase64String(credentialBytes));

      using var response = await ManagementApiClient.SendAsync(
          request,
          HttpCompletionOption.ResponseHeadersRead,
          cancellationToken).ConfigureAwait(false);

      if (!response.IsSuccessStatusCode)
      {
        failures.Add(
            $"RabbitMQ Management API binding inspection returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
        return;
      }

      var actualBindings = await response.Content
          .ReadFromJsonAsync<List<RabbitMQBinding>>(cancellationToken: cancellationToken)
          .ConfigureAwait(false);

      if (actualBindings is null)
      {
        failures.Add("RabbitMQ Management API returned an empty or invalid binding inventory");
        return;
      }

      foreach (var expected in GetExpectedBindings())
      {
        var actual = actualBindings.FirstOrDefault(binding =>
            string.Equals(binding.Source, expected.Source, StringComparison.Ordinal) &&
            string.Equals(binding.Destination, expected.Destination, StringComparison.Ordinal) &&
            string.Equals(binding.DestinationType, expected.DestinationType, StringComparison.Ordinal) &&
            string.Equals(binding.RoutingKey, expected.RoutingKey, StringComparison.Ordinal));

        if (actual is null)
        {
          failures.Add(
              $"required binding '{expected.Source}' -> '{expected.Destination}' with routing key '{expected.RoutingKey}' is missing");
          continue;
        }

        foreach (var (argumentName, expectedValue) in expected.Arguments)
        {
          if (!actual.Arguments.TryGetValue(argumentName, out var actualValue) ||
              actualValue.ValueKind != JsonValueKind.String ||
              !string.Equals(actualValue.GetString(), expectedValue, StringComparison.Ordinal))
          {
            failures.Add(
                $"binding '{expected.Source}' -> '{expected.Destination}' has an incorrect or missing '{argumentName}' argument");
          }
        }

        if (actual.Arguments.Count != expected.Arguments.Count)
          failures.Add(
              $"binding '{expected.Source}' -> '{expected.Destination}' has unexpected binding arguments");
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
    {
      failures.Add($"RabbitMQ Management API binding inspection timed out: {ex.Message}");
    }
    catch (HttpRequestException ex)
    {
      failures.Add($"RabbitMQ Management API binding inspection failed: {ex.Message}");
    }
    catch (JsonException ex)
    {
      failures.Add($"RabbitMQ Management API returned an invalid binding inventory: {ex.Message}");
    }
  }

  private IReadOnlyCollection<ExpectedBinding> GetExpectedBindings()
  {
    var bindings = new List<ExpectedBinding>
    {
      new(_deadLetterExchangeName, _deadLetterQueueName, "queue", _deadLetterQueueName,
          new Dictionary<string, string>(StringComparer.Ordinal))
    };

    if (_options.ReplayStrategy == RabbitMQReplayStrategy.ProgressiveFourStep)
    {
      var replayExchange = RabbitMQExchangeNamer.GetReplayExchangeName(
          _assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null."));

      for (var retryNumber = 1; retryNumber <= 3; retryNumber++)
      {
        bindings.Add(new ExpectedBinding(
            replayExchange,
            $"{_queueName}-retry-{retryNumber}",
            "queue",
            ProgressiveFourStepReplayStrategy.GetRetryRoutingKey(_queueName, retryNumber),
            new Dictionary<string, string>(StringComparer.Ordinal)));
      }
    }

    var integrationEvents = Franz.Common.Messaging.Manifest.MessagingManifestRegistry.GetAll()
        .SelectMany(static manifest => manifest.Subscriptions)
        .Distinct();

    foreach (var eventType in integrationEvents)
    {
      var sourceExchange = RabbitMQExchangeNamer.GetEventExchangeName(eventType.Assembly);
      var eventName = HeaderNamer.GetEventClassName(eventType);
      bindings.Add(new ExpectedBinding(
          sourceExchange,
          _queueName,
          "queue",
          string.Empty,
          new Dictionary<string, string>(StringComparer.Ordinal)
          {
            [MessagingConstants.ClassName] = eventName,
            ["x-match"] = "all"
          }));
    }

    return bindings;
  }

  private (string? Username, string? Password) ResolveManagementCredentials()
  {
    var username = _options.ManagementApiUsername ?? _options.UserName;
    var password = _options.ManagementApiPassword ?? _options.Password;

    if ((!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password)) ||
        string.IsNullOrWhiteSpace(_options.BootStrapServers) ||
        !Uri.TryCreate(_options.BootStrapServers, UriKind.Absolute, out var amqpUri))
      return (username, password);

    var credentials = amqpUri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
      return (username, password);

    return (
        string.IsNullOrWhiteSpace(username) ? Uri.UnescapeDataString(credentials[0]) : username,
        string.IsNullOrWhiteSpace(password) ? Uri.UnescapeDataString(credentials[1]) : password);
  }

  private string ResolveManagementVirtualHost()
  {
    if (!string.IsNullOrWhiteSpace(_options.ManagementApiVirtualHost))
      return _options.ManagementApiVirtualHost;

    if (!string.IsNullOrWhiteSpace(_options.VirtualHost))
      return _options.VirtualHost;

    if (!string.IsNullOrWhiteSpace(_options.BootStrapServers) &&
        Uri.TryCreate(_options.BootStrapServers, UriKind.Absolute, out var amqpUri))
    {
      var path = Uri.UnescapeDataString(amqpUri.AbsolutePath.Trim('/'));
      return string.IsNullOrWhiteSpace(path) ? "/" : path;
    }

    return "/";
  }

  private sealed record ExpectedBinding(
      string Source,
      string Destination,
      string DestinationType,
      string RoutingKey,
      IReadOnlyDictionary<string, string> Arguments);

  private sealed class RabbitMQBinding
  {
    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; init; } = string.Empty;

    [JsonPropertyName("destination_type")]
    public string DestinationType { get; init; } = string.Empty;

    [JsonPropertyName("routing_key")]
    public string RoutingKey { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, JsonElement> Arguments { get; init; } = new(StringComparer.Ordinal);
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