#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

/// <summary>
/// Direct Point-to-Point Sender for RabbitMQ.
/// Senior Note: Utilizes the Default Exchange with Queue-based routing keys.
/// </summary>
public sealed class RabbitMQMessagingSender(
    Connections.ChannelPool channelPool,
    IMessageHandler handler,
    IMessagingTransaction? transaction = null) : IMessagingSender, IAsyncDisposable, IDisposable
{
  public void Dispose()
  {
    // Sync disposal for IDisposable interface compliance.
    GC.SuppressFinalize(this);
  }

  public ValueTask DisposeAsync()
  {
    Dispose();
    return ValueTask.CompletedTask;
  }

  public async ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(message.Body);

    // 1. Pipeline execution (Applies Headers, Correlation, etc.)
    await handler.ProcessAsync(message);

    // 2. Type-Safe Queue Resolution
    // FIX: Removed ?? because MessageKind is a non-nullable value type. 
    // If message.Kind needs to be optional, the 'Message' class property must be 'MessageKind?'.
    var queue = QueueNamer.GetQueueName(message.Kind);

    // 3. Resource Acquisition
    var channel = await channelPool.GetAsync(cancellationToken).ConfigureAwait(false);

    try
    {
      // 4. Transaction-to-Channel Bridge
      if (transaction is RabbitMQMessagingTransaction rmt)
      {
        rmt.Attach(channel);
      }

      await transaction?.BeginAsync();

      var body = Encoding.UTF8.GetBytes(message.Body);

      // 5. Header Mapping (RabbitMQ v7 compatible)
      var properties = message.Headers.ToBasicProperties() as BasicProperties;

      // 6. Direct Publish
      await channel.BasicPublishAsync(
          exchange: string.Empty,
          routingKey: queue,
          mandatory: true,
          basicProperties: properties!,
          body: body,
          cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      await transaction?.CompleteAsync();
    }
    catch (Exception)
    {
     await transaction?.RollbackAsync();
      throw;
    }
    finally
    {
      // 7. Resource Release
      channelPool.Return(channel);
    }
  }
}