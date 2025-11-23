using System.Text.Json;
using Franz.Common.Messaging.RabbitMQ.Connections;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;



public sealed class RabbitMqMessageModel : IRabbitMqMessageModel
{
  private readonly IConnectionProvider _connectionProvider;
  private readonly IConnection _connection;
  private readonly IChannel _channel;

  public RabbitMqMessageModel(IConnectionProvider connectionProvider)
  {
    _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));

    _connection = _connectionProvider.Current;
    // you can switch to CreateChannelAsync if you want pure async
    _channel = (IChannel)_connection.CreateChannelAsync();
  }

  public async Task ProduceAsync<TMessage>(
      string exchange,
      string routingKey,
      TMessage message,
      CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new ArgumentNullException(nameof(message));

    // Your envelope type – assuming you add this somewhere in Franz.Common.Messaging
    var envelope = FranzMessageEnvelope<TMessage>.Create(message);

    var bytes = JsonSerializer.SerializeToUtf8Bytes(
        envelope,
        new JsonSerializerOptions
        {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
          WriteIndented = false
        });

    // RabbitMQ.Client 7.x:
    // easiest path is to use the extension method that only takes body
    // and lets RabbitMQ set default properties. Our envelope already
    // carries MessageId / Timestamp / Type etc.
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
      await _channel.CloseAsync();
    }
    catch
    {
      // ignore on dispose
    }

    await _channel.DisposeAsync();
    await _connection.DisposeAsync();
  }

  public void Dispose()
  {
    DisposeAsync().AsTask().GetAwaiter().GetResult();
  }
}
