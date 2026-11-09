#nullable enable
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class ProgressiveFourStepReplayStrategy : IReplayStrategy
{
  public const string ReplayHeader = "x-replay";
  public const string DelayHeader = "x-delay";
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";
  private const int MaxHeaderTextLength = 4_096;
  private readonly IConfirmChannelPool _confirmChannelPool;
  private readonly int _publisherConfirmTimeoutSeconds;
  private readonly string _queueName;
  private readonly string _replayExchange;
  private readonly string _deadLetterQueue;
  private readonly int _maxRetryAttempts = 3;
  private readonly int? _retryBackoffMilliseconds;
  private readonly bool _deadLetterOnExhaustion = true;

  public ProgressiveFourStepReplayStrategy(
      IConfirmChannelPool confirmChannelPool,
      IAssemblyAccessor accessor,
      IOptions<RabbitMQMessagingOptions> options)
  {
    _confirmChannelPool = confirmChannelPool ?? throw new ArgumentNullException(nameof(confirmChannelPool));
    ArgumentNullException.ThrowIfNull(accessor);
    ArgumentNullException.ThrowIfNull(options);
    var settings = options.Value;
    var entry = accessor.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly cannot be null.");
    _queueName = settings.QueueName ?? RabbitMQQueueNamer.GetQueueName(entry);
    _replayExchange = RabbitMQExchangeNamer.GetReplayExchangeName(entry);
    _deadLetterQueue = settings.DeadLetterQueueName ?? RabbitMQQueueNamer.GetDeadLetterQueueName(entry);
    _publisherConfirmTimeoutSeconds = settings.PublisherConfirmTimeoutSeconds;
    if (settings.FailurePolicy is { } policy)
    {
      _maxRetryAttempts = policy.MaxRetryAttempts;
      _retryBackoffMilliseconds = checked((int)Math.Ceiling(policy.RetryBackoff.TotalMilliseconds));
      _deadLetterOnExhaustion = policy.DeadLetterOnExhaustion;
    }
  }

  public async Task ReplayAsync(BasicDeliverEventArgs e, Exception ex)
  {
    ArgumentNullException.ThrowIfNull(e);
    ArgumentNullException.ThrowIfNull(ex);

    var headers = e.BasicProperties.Headers is null
        ? new Dictionary<string, object?>()
        : new Dictionary<string, object?>(e.BasicProperties.Headers);
    var replayCount = headers.TryGetValue(ReplayHeader, out var value)
        ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) + 1
        : 1;
    headers[ReplayHeader] = replayCount;
    var properties = RabbitMQReplayPropertiesMapper.Create(e, headers);

    string exchange;
    string routingKey;
    if (replayCount > _maxRetryAttempts)
    {
      if (!_deadLetterOnExhaustion)
        return;

      headers[ExceptionMessageHeader] = Truncate(ex.Message);
      headers[ExceptionStackTraceHeader] = Truncate(ex.StackTrace ?? string.Empty);
      exchange = string.Empty;
      routingKey = _deadLetterQueue;
    }
    else
    {
      exchange = _replayExchange;
      // Retry count remains policy-driven, while the broker topology reuses
      // the third (longest) delay stage for every subsequent retry.
      routingKey = GetRetryRoutingKey(_queueName, Math.Min(replayCount, 3));
    }

    var channel = await _confirmChannelPool.GetPublisherConfirmChannelAsync().ConfigureAwait(false);

    var published = false;
    using var confirmationTimeout = new CancellationTokenSource(
        TimeSpan.FromSeconds(_publisherConfirmTimeoutSeconds));
    try
    {
      // On a confirm-enabled channel RabbitMQ.Client completes this operation only after
      // publisher confirmation and raises PublishException for mandatory unroutable messages.
      await channel.BasicPublishAsync(exchange, routingKey, mandatory: true,
          basicProperties: properties, body: e.Body,
          cancellationToken: confirmationTimeout.Token).ConfigureAwait(false);
      published = true;
    }
    finally
    {
      if (published)
        _confirmChannelPool.ReturnPublisherConfirmChannel(channel);
      else
        await DisposeChannelAsync(channel).ConfigureAwait(false);
    }
  }

  internal int GetConfiguredRetryDelayMilliseconds(int retryNumber)
      => _retryBackoffMilliseconds ?? GetRetryDelayMilliseconds(retryNumber);

  internal int MaxRetryAttempts => _maxRetryAttempts;

  internal static string GetRetryRoutingKey(string queueName, int retryNumber) => $"{queueName}-retry-{retryNumber}";
  internal static int GetRetryDelayMilliseconds(int retryNumber)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryNumber);

    // Keep the progressive schedule bounded when a policy configures more than
    // the three default retry stages. Custom policies can still provide a fixed
    // RetryBackoff that is applied by the topology initializer.
    return retryNumber switch
    {
      1 => 1_000,
      2 => 10_000,
      _ => 60_000
    };
  }

  private static string Truncate(string value) => value.Length <= MaxHeaderTextLength ? value : value[..MaxHeaderTextLength];

  private static async ValueTask DisposeChannelAsync(IChannel channel)
  {
    try
    {
      if (channel.IsOpen)
        await channel.CloseAsync().ConfigureAwait(false);
    }
    catch { /* Preserve the publication failure. */ }

    try
    {
      await channel.DisposeAsync().ConfigureAwait(false);
    }
    catch { /* Preserve the publication failure. */ }
  }
}
