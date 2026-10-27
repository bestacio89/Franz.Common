#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingSender(
    Connections.IChannelPool channelPool,
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
      if (transaction is RabbitMQMessagingTransaction rabbitTransaction)
        rabbitTransaction.Attach(channel);

      if (transaction is not null)
        await transaction.BeginAsync(cancellationToken).ConfigureAwait(false);

      var body = Encoding.UTF8.GetBytes(message.Body);
      var properties = message.Headers.ToBasicProperties();
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
          cancellationToken: cancellationToken).ConfigureAwait(false);

      if (transaction is not null)
        await transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (Exception sendException)
    {
      if (transaction is not null)
      {
        try
        {
          await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
          throw new AggregateException(
              "RabbitMQ send failed and the transaction rollback also failed.",
              sendException,
              rollbackException);
        }
      }

      throw;
    }
    finally
    {
      if (transaction is RabbitMQMessagingTransaction)
        await DisposeTransactionalChannelAsync(channel).ConfigureAwait(false);
      else
        channelPool.Return(channel);
    }
  }

  private static async ValueTask DisposeTransactionalChannelAsync(IChannel channel)
  {
    try
    {
      if (channel.IsOpen)
        await channel.CloseAsync().ConfigureAwait(false);
    }
    catch
    {
      // A channel in transaction mode must never return to the shared pool.
    }

    try
    {
      await channel.DisposeAsync().ConfigureAwait(false);
    }
    catch
    {
      // Preserve the send/transaction outcome if cleanup also fails.
    }
  }
}