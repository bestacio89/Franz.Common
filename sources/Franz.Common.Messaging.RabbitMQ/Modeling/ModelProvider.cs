using Franz.Common.Messaging.RabbitMQ.Connections;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;



public sealed class ModelProvider : IModelProvider, IAsyncDisposable, IDisposable
{
  private readonly IConnectionProvider _connectionProvider;
  private IChannel? _channel;

  public ModelProvider(IConnectionProvider connectionProvider)
  {
    _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
  }

  public IChannel Current => _channel ??= CreateChannel();

  private IChannel CreateChannel()
  {
    // ConnectionFactoryProvider already builds the ConnectionFactory
    // and exposes IConnection via IConnectionProvider.Current
    var connection = _connectionProvider.Current;

    // RabbitMQ 7.x: CreateChannel / CreateChannelAsync
    // sync variant is still available
    return (IChannel)connection.CreateChannelAsync();
  }

  public async ValueTask DisposeAsync()
  {
    if (_channel is null)
      return;

    try
    {
      // Close via async extension methods in 7.x
      await _channel.CloseAsync();
    }
    catch
    {
      // swallow on dispose
    }

    await _channel.DisposeAsync();
    _channel = null;
  }

  void IDisposable.Dispose()
  {
    // bridge sync dispose to async
    DisposeAsync().AsTask().GetAwaiter().GetResult();
  }
}
