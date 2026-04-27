#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingSender(
    Connections.ChannelPool channelPool,
    IMessageHandler handler,
    IMessagingTransaction? transaction = null)
    : IMessagingSender, IAsyncDisposable, IDisposable
{
  public void Dispose()
  {
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

    await handler.ProcessAsync(message, cancellationToken).ConfigureAwait(false);

    var queue = QueueNamer.GetQueueName(message.Kind);

    var channel = await channelPool.GetAsync(cancellationToken).ConfigureAwait(false);

    try
    {
      if (transaction is RabbitMQMessagingTransaction rmt)
      {
        rmt.Attach(channel);
      }

      if (transaction is not null)
      {
        await transaction.BeginAsync().ConfigureAwait(false);
      }

      var body = Encoding.UTF8.GetBytes(message.Body);

      // ✅ FIX 1: remove unsafe cast + force null safety explicitly
      var properties = message.Headers.ToBasicProperties();

      // Ensure non-null BasicProperties
      var basicProperties = properties ?? new BasicProperties
      {
        Headers = new Dictionary<string, object?>()
      };

      await channel.BasicPublishAsync(
          exchange: string.Empty,
          routingKey: queue,
          mandatory: true,
          basicProperties: basicProperties,
          body: body,
          cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (transaction is not null)
      {
        await transaction.CompleteAsync().ConfigureAwait(false);
      }
    }
    catch
    {
      if (transaction is not null)
      {
        await transaction.RollbackAsync().ConfigureAwait(false);
      }
      throw;
    }
    finally
    {
      channelPool.Return(channel);
    }
  }
}