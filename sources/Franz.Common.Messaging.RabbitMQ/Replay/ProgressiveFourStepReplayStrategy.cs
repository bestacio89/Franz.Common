#nullable enable
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Franz.Common.Messaging;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class ProgressiveFourStepReplayStrategy : IReplayStrategy
{
  public const string ReplayHeader = "x-replay";
  public const string DelayHeader = "x-delay";
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";
  private const int MaxHeaderTextLength = 4_096;
  private readonly IModelProvider? _legacyProvider;
  private readonly IConfirmChannelPool? _confirmChannelPool;
  private readonly string _queueName;
  private readonly string _replayExchange;
  private readonly string _deadLetterQueue;

  // Compatibility constructor for existing consumers. Framework DI uses the confirmed-channel overload.
  public ProgressiveFourStepReplayStrategy(IModelProvider provider, IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    _legacyProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    (_queueName, _replayExchange, _deadLetterQueue) = ResolveTopology(accessor, options);
  }

  public ProgressiveFourStepReplayStrategy(IConfirmChannelPool confirmChannelPool, IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    _confirmChannelPool = confirmChannelPool ?? throw new ArgumentNullException(nameof(confirmChannelPool));
    (_queueName, _replayExchange, _deadLetterQueue) = ResolveTopology(accessor, options);
  }

  private static (string Queue, string ReplayExchange, string DeadLetterQueue) ResolveTopology(
      IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    ArgumentNullException.ThrowIfNull(accessor);
    ArgumentNullException.ThrowIfNull(options);
    var entry = accessor.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly cannot be null.");
    return (QueueNamer.GetQueueName(entry), ExchangeNamer.GetReplayExchangeName(entry),
        QueueNamer.GetDeadLetterQueueName(entry));
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
    var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent, Headers = headers };

    string exchange;
    string routingKey;
    if (replayCount >= 4)
    {
      headers[ExceptionMessageHeader] = Truncate(ex.Message);
      headers[ExceptionStackTraceHeader] = Truncate(ex.StackTrace ?? string.Empty);
      exchange = string.Empty;
      routingKey = _deadLetterQueue;
    }
    else
    {
      exchange = _replayExchange;
      routingKey = GetRetryRoutingKey(_queueName, replayCount);
    }

    IChannel channel;
    var confirmed = _confirmChannelPool is not null;
    channel = confirmed
        ? await _confirmChannelPool!.GetPublisherConfirmChannelAsync().ConfigureAwait(false)
        : await _legacyProvider!.GetChannelAsync().ConfigureAwait(false);

    var published = false;
    try
    {
      // On a confirm-enabled channel RabbitMQ.Client completes this operation only after
      // publisher confirmation and raises PublishException for mandatory unroutable messages.
      await channel.BasicPublishAsync(exchange, routingKey, mandatory: true,
          basicProperties: properties, body: e.Body).ConfigureAwait(false);
      published = true;
    }
    finally
    {
      if (confirmed)
      {
        if (published)
          _confirmChannelPool!.ReturnPublisherConfirmChannel(channel);
        else
          await DisposeChannelAsync(channel).ConfigureAwait(false);
      }
    }
  }

  internal static string GetRetryRoutingKey(string queueName, int retryNumber) => $"{queueName}-retry-{retryNumber}";
  internal static int GetRetryDelayMilliseconds(int retryNumber) => retryNumber switch
  {
    1 => 1_000,
    2 => 10_000,
    3 => 60_000,
    _ => throw new ArgumentOutOfRangeException(nameof(retryNumber))
  };

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
