#nullable enable
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Modeling;
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
    {
      await asyncDisposable.DisposeAsync();
    }
  }

  public async ValueTask Publish<TIntegrationEvent>(TIntegrationEvent evt, CancellationToken ct = default)
      where TIntegrationEvent : IIntegrationEvent
  {
    // Ensure transport is initialized
    await _initializer.InitializeAsync(ct);

    // Build message and process through handler pipeline
    var message = _messageFactory.Build(evt);

    // Execute pipeline (Middleware/Validation)
    await _handler.ProcessAsync(message);

    var exchange = _options.ExchangeName ?? ExchangeNamer.GetEventExchangeName(evt.GetType().Assembly);

    await PublishInternalAsync(message, exchange, ct);
  }

  private async ValueTask PublishInternalAsync(Message message, string exchange, CancellationToken ct)
  {
    // Use SemaphoreSlim double-check is handled inside the Pool
    var channel = await _channelPool.GetAsync(ct);

    try
    {
      if (_transaction is not null)
      {
        await _transaction.BeginAsync(ct);
      }

      var body = Encoding.UTF8.GetBytes(message.Body ?? string.Empty);

      // ToBasicProperties should return the interface IBasicProperties to avoid Moq generic issues
      var properties = message.Headers.ToBasicProperties();

      await channel.BasicPublishAsync(
          exchange: exchange,
          routingKey: _options.DefaultRoutingKey ?? string.Empty,
          mandatory: true,
          basicProperties: properties,
          body: body,
          cancellationToken: ct
      );

      if (_transaction is not null)
      {
        await _transaction.CompleteAsync(ct);
      }
    }
    catch
    {
      if (_transaction is not null)
      {
        await _transaction.RollbackAsync(ct);
      }
      throw;
    }
    finally
    {
      _channelPool.Return(channel);
    }
  }
}