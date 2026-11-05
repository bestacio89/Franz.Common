#nullable enable
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

public class NoReplayStrategy : IReplayStrategy
{
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";
  private const int MaxHeaderTextLength = 4_096;
  private readonly IConfirmChannelPool _confirmChannelPool;
  private readonly int _publisherConfirmTimeoutSeconds;
  private readonly string _deadLetterQueue;
  private readonly bool _deadLetterOnExhaustion = true;

  public NoReplayStrategy(
      IConfirmChannelPool confirmChannelPool,
      IAssemblyAccessor accessor,
      IOptions<RabbitMQMessagingOptions> options)
  {
    _confirmChannelPool = confirmChannelPool ?? throw new ArgumentNullException(nameof(confirmChannelPool));
    ArgumentNullException.ThrowIfNull(accessor);
    ArgumentNullException.ThrowIfNull(options);
    var settings = options.Value;
    var entry = accessor.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly cannot be null.");
    _deadLetterQueue = settings.DeadLetterQueueName ?? RabbitMQQueueNamer.GetDeadLetterQueueName(entry);
    _publisherConfirmTimeoutSeconds = settings.PublisherConfirmTimeoutSeconds;
    _deadLetterOnExhaustion = settings.FailurePolicy?.DeadLetterOnExhaustion ?? true;
  }

  public async Task ReplayAsync(BasicDeliverEventArgs e, Exception ex)
  {
    ArgumentNullException.ThrowIfNull(e);
    ArgumentNullException.ThrowIfNull(ex);
    if (!_deadLetterOnExhaustion)
      return;

    var headers = e.BasicProperties.Headers is null
        ? new Dictionary<string, object?>()
        : new Dictionary<string, object?>(e.BasicProperties.Headers);
    headers[ExceptionMessageHeader] = Truncate(ex.Message);
    headers[ExceptionStackTraceHeader] = Truncate(ex.StackTrace ?? string.Empty);

    var properties = new BasicProperties
    {
      DeliveryMode = DeliveryModes.Persistent,
      Headers = headers
    };

    var channel = await _confirmChannelPool.GetPublisherConfirmChannelAsync().ConfigureAwait(false);

    var published = false;
    using var confirmationTimeout = new CancellationTokenSource(
        TimeSpan.FromSeconds(_publisherConfirmTimeoutSeconds));
    try
    {
      await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _deadLetterQueue,
          mandatory: true, basicProperties: properties, body: e.Body,
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

  private static string Truncate(string value)
      => value.Length <= MaxHeaderTextLength ? value : value[..MaxHeaderTextLength];

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
