#nullable enable
using Franz.Common.Messaging.Configuration;
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
  // Retained for binary/source compatibility with existing consumers.
  public const string DelayHeader = "x-delay";
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";
  private const int MaxHeaderTextLength = 4_096;
  private readonly IModelProvider _provider;
  private readonly string _queueName;
  private readonly string _replayExchange;
  private readonly string _deadLetterQueue;

  public ProgressiveFourStepReplayStrategy(IModelProvider provider, IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    ArgumentNullException.ThrowIfNull(accessor);
    ArgumentNullException.ThrowIfNull(options);
    var entry = accessor.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly cannot be null.");
    _queueName = QueueNamer.GetQueueName(entry);
    _replayExchange = ExchangeNamer.GetReplayExchangeName(entry);
    _deadLetterQueue = QueueNamer.GetDeadLetterQueueName(entry);
  }

  public async Task ReplayAsync(BasicDeliverEventArgs e, Exception ex)
  {
    ArgumentNullException.ThrowIfNull(e);
    ArgumentNullException.ThrowIfNull(ex);
    var channel = await _provider.GetChannelAsync().ConfigureAwait(false);
    var headers = e.BasicProperties.Headers is null
        ? new Dictionary<string, object?>()
        : new Dictionary<string, object?>(e.BasicProperties.Headers);
    var replayCount = headers.TryGetValue(ReplayHeader, out var value)
        ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) + 1
        : 1;
    headers[ReplayHeader] = replayCount;
    var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent, Headers = headers };

    if (replayCount >= 4)
    {
      headers[ExceptionMessageHeader] = Truncate(ex.Message);
      headers[ExceptionStackTraceHeader] = Truncate(ex.StackTrace ?? string.Empty);
      await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _deadLetterQueue, mandatory: true,
          basicProperties: properties, body: e.Body).ConfigureAwait(false);
      return;
    }

    await channel.BasicPublishAsync(exchange: _replayExchange, routingKey: GetRetryRoutingKey(_queueName, replayCount),
        mandatory: true, basicProperties: properties, body: e.Body).ConfigureAwait(false);
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
}