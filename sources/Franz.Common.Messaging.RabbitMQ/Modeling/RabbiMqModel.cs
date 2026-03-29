using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public sealed class RabbitMqMessageModel
  : IRabbitMqMessageModel, IAsyncDisposable, IDisposable
{
  private readonly IChannel _channel;

  public RabbitMqMessageModel(IChannel channel)
  {
    _channel = channel;
  }

  public async ValueTask ProduceAsync<TMessage>(
       string exchange,
       string routingKey,
       TMessage message,
       CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    var envelope = FranzMessageEnvelope<TMessage>.Create(message);

    var options = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false,
      TypeInfoResolver = RabbitMqJsonSerializerContext.Default
    };

    var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, options);

    // Explicitly provide BasicProperties as the type argument
    await _channel.BasicPublishAsync<BasicProperties>(
      exchange: exchange,
      routingKey: routingKey,
      mandatory: true,
      basicProperties: null, // Or pass new BasicProperties() if metadata is required
      body: bytes,
      cancellationToken: cancellationToken);
  }

  public async ValueTask DisposeAsync()
  {
    try
    {
      if (_channel.IsOpen)
        await _channel.CloseAsync();
    }
    catch
    {
      // swallow on dispose
    }

    await _channel.DisposeAsync();
  }

  void IDisposable.Dispose()
    => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
