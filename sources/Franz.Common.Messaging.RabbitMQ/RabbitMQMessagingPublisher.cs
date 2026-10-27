#nullable enable
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ;

public sealed class RabbitMQMessagingPublisher(
    IChannelPool channelPool,
    IMessagingInitializer initializer,
    IMessageFactory messageFactory,
    IMessageHandler handler,
    IOptions<RabbitMQMessagingOptions> options,
    IMessagingTransaction? transaction = null
) : IMessagingPublisher, IAsyncDisposable
{
  private readonly IChannelPool _channelPool = channelPool;
  private readonly IMessagingInitializer _initializer = initializer;
  private readonly IMessageFactory _messageFactory = messageFactory;
  private readonly IMessageHandler _handler = handler;
  private readonly RabbitMQMessagingOptions _options = options.Value;
  private readonly IMessagingTransaction? _transaction = transaction;

  public async ValueTask DisposeAsync()
  {
    if (_transaction is IAsyncDisposable asyncDisposable)
      await asyncDisposable.DisposeAsync().ConfigureAwait(false);
  }

  public async ValueTask Publish<TIntegrationEvent>(TIntegrationEvent evt, CancellationToken ct = default)
      where TIntegrationEvent : IIntegrationEvent
  {
    await _initializer.InitializeAsync(ct).ConfigureAwait(false);

    var message = _messageFactory.Build(evt);
    await _handler.ProcessAsync(message, ct).ConfigureAwait(false);

    var exchange = _options.ExchangeName ?? ExchangeNamer.GetEventExchangeName(evt.GetType().Assembly);
    await PublishInternalAsync(message, exchange, ct).ConfigureAwait(false);
  }

  private async ValueTask PublishInternalAsync(Message message, string exchange, CancellationToken ct)
  {
    var channel = await _channelPool.GetAsync(ct).ConfigureAwait(false);

    try
    {
      if (_transaction is RabbitMQMessagingTransaction rabbitTransaction)
        rabbitTransaction.Attach(channel);

      if (_transaction is not null)
        await _transaction.BeginAsync(ct).ConfigureAwait(false);

      var body = Encoding.UTF8.GetBytes(message.Body ?? string.Empty);
      var properties = message.Headers.ToBasicProperties();

      await channel.BasicPublishAsync(
          exchange: exchange,
          routingKey: _options.DefaultRoutingKey ?? string.Empty,
          mandatory: true,
          basicProperties: properties,
          body: body,
          cancellationToken: ct).ConfigureAwait(false);

      if (_transaction is not null)
        await _transaction.CompleteAsync(ct).ConfigureAwait(false);
    }
    catch (Exception publishException)
    {
      if (_transaction is not null)
      {
        try
        {
          await _transaction.RollbackAsync(ct).ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
          throw new AggregateException(
              "RabbitMQ publishing failed and the transaction rollback also failed.",
              publishException,
              rollbackException);
        }
      }

      throw;
    }
    finally
    {
      if (_transaction is RabbitMQMessagingTransaction)
        await DisposeTransactionalChannelAsync(channel).ConfigureAwait(false);
      else
        _channelPool.Return(channel);
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
      // Preserve the publish/transaction outcome if cleanup also fails.
    }
  }
}