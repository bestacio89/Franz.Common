using System.Text.Json;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public sealed class RabbitMqMessageModel
  : IRabbitMqMessageModel, IAsyncDisposable, IDisposable
{
  private readonly IChannel _channel;

  public RabbitMqMessageModel(IModelProvider modelProvider)
  {
    if (modelProvider is null)
      throw new ArgumentNullException(nameof(modelProvider));

    _channel = modelProvider.Current;
  }

  public async Task ProduceAsync<TMessage>(
      string exchange,
      string routingKey,
      TMessage message,
      CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new ArgumentNullException(nameof(message));

    var envelope = FranzMessageEnvelope<TMessage>.Create(message);

    var bytes = JsonSerializer.SerializeToUtf8Bytes(
      envelope,
      new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
      });

    await _channel.BasicPublishAsync(
      exchange,
      routingKey,
      bytes,
      cancellationToken);
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
