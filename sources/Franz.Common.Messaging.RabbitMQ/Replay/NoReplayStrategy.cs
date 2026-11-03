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

public class NoReplayStrategy : IReplayStrategy
{
  public const string ExceptionMessageHeader = "x-exception-message";
  public const string ExceptionStackTraceHeader = "x-exception-stacktrace";
  private const int MaxHeaderTextLength = 4_096;
  private readonly IModelProvider? _legacyProvider;
  private readonly IConfirmChannelPool? _confirmChannelPool;
  private readonly string _deadLetterQueue;

  // Compatibility constructor for existing consumers. Framework DI uses the confirmed-channel overload.
  public NoReplayStrategy(IModelProvider provider, IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    _legacyProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    _deadLetterQueue = ResolveDeadLetterQueue(accessor, options);
  }

  public NoReplayStrategy(IConfirmChannelPool confirmChannelPool, IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    _confirmChannelPool = confirmChannelPool ?? throw new ArgumentNullException(nameof(confirmChannelPool));
    _deadLetterQueue = ResolveDeadLetterQueue(accessor, options);
  }

  private static string ResolveDeadLetterQueue(IAssemblyAccessor accessor, IOptions<MessagingOptions> options)
  {
    ArgumentNullException.ThrowIfNull(accessor);
    ArgumentNullException.ThrowIfNull(options);
    var entry = accessor.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly cannot be null.");
    return QueueNamer.GetDeadLetterQueueName(entry);
  }

  public async Task ReplayAsync(BasicDeliverEventArgs e, Exception ex)
  {
    ArgumentNullException.ThrowIfNull(e);
    ArgumentNullException.ThrowIfNull(ex);

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

    var confirmed = _confirmChannelPool is not null;
    var channel = confirmed
        ? await _confirmChannelPool!.GetPublisherConfirmChannelAsync().ConfigureAwait(false)
        : await _legacyProvider!.GetChannelAsync().ConfigureAwait(false);

    var published = false;
    try
    {
      await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _deadLetterQueue,
          mandatory: true, basicProperties: properties, body: e.Body).ConfigureAwait(false);
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
