#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingSender : IMessagingSender, IAsyncDisposable, IDisposable
{
  private readonly Connections.IChannelPool _channelPool;
  private readonly IMessageHandler _handler;
  private readonly IMessagingTransaction? _transaction;
  private readonly IOptions<RabbitMQMessagingOptions> _options;

  public RabbitMQMessagingSender(
      Connections.IChannelPool channelPool,
      IMessageHandler handler,
      IMessagingTransaction? transaction,
      IOptions<RabbitMQMessagingOptions> options)
  {
    _channelPool = channelPool ?? throw new ArgumentNullException(nameof(channelPool));
    _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    _transaction = transaction;
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  public void Dispose() => GC.SuppressFinalize(this);

  public ValueTask DisposeAsync()
  {
    Dispose();
    return ValueTask.CompletedTask;
  }

  public async ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    if (message.Body is null)
      throw new PermanentMessagingException($"RabbitMQ message {message.Id} has no body; retrying the same payload cannot resolve this failure.");

    await _handler.ProcessAsync(message, cancellationToken).ConfigureAwait(false);
    var queue = RabbitMQQueueNamer.GetQueueName(message.Kind);

    var advancedPool = _channelPool as Connections.IConfirmChannelPool;
    var dedicatedChannel = advancedPool is not null;
    var channel = _transaction is RabbitMQMessagingTransaction && advancedPool is not null
        ? await advancedPool.GetTransactionalChannelAsync(cancellationToken).ConfigureAwait(false)
        : advancedPool is not null
            ? await advancedPool.GetPublisherConfirmChannelAsync(cancellationToken).ConfigureAwait(false)
            : await _channelPool.GetAsync(cancellationToken).ConfigureAwait(false);

    using var confirmationTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    confirmationTimeout.CancelAfter(TimeSpan.FromSeconds(_options.Value.PublisherConfirmTimeoutSeconds));
    var publishCompleted = false;

    try
    {
      if (_transaction is RabbitMQMessagingTransaction rabbitTransaction)
        rabbitTransaction.Attach(channel);

      if (_transaction is not null)
        await _transaction.BeginAsync(cancellationToken).ConfigureAwait(false);

      var body = Encoding.UTF8.GetBytes(message.Body);
      var properties = message.ToBasicProperties();
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
          cancellationToken: dedicatedChannel ? confirmationTimeout.Token : cancellationToken).ConfigureAwait(false);

      if (_transaction is not null)
        await _transaction.CompleteAsync(cancellationToken).ConfigureAwait(false);

      publishCompleted = true;
    }
    catch (Exception sendException)
    {
      if (_transaction is not null)
      {
        try
        {
          await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
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
      if (_transaction is RabbitMQMessagingTransaction)
        await DisposeChannelAsync(channel).ConfigureAwait(false);
      else if (advancedPool is not null)
      {
        if (publishCompleted)
          advancedPool.ReturnPublisherConfirmChannel(channel);
        else
          await DisposeChannelAsync(channel).ConfigureAwait(false);
      }
      else if (publishCompleted)
        _channelPool.Return(channel);
      else
        await DisposeChannelAsync(channel).ConfigureAwait(false);
    }
  }

  private static async ValueTask DisposeChannelAsync(IChannel channel)
  {
    try
    {
      if (channel.IsOpen)
        await channel.CloseAsync().ConfigureAwait(false);
    }
    catch
    {
      // A confirmed or transactional channel must never return to the shared pool.
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
