using Franz.Common.Messaging.RabbitMQ.Connections;
using RabbitMQ.Client;
using System.Threading;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public sealed class ModelProvider : IModelProvider, IAsyncDisposable, IDisposable
{
  private readonly IConnectionProvider _connectionProvider;
  private readonly SemaphoreSlim _sync = new(1, 1);

  private IChannel? _channel;
  private bool _disposed;

  public ModelProvider(IConnectionProvider connectionProvider)
  {
    _connectionProvider = connectionProvider
      ?? throw new ArgumentNullException(nameof(connectionProvider));
  }

  public IChannel Current
  {
    get
    {
      ThrowIfDisposed();
      return GetOrCreateChannelAsync()
        .GetAwaiter()
        .GetResult();
    }
  }

  private async Task<IChannel> GetOrCreateChannelAsync()
  {
    if (_channel is { IsOpen: true })
      return _channel;

    await _sync.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_channel is { IsOpen: true })
        return _channel;

      var connection = _connectionProvider.Current;

      // 🔑 Correct: await async channel creation
      _channel = await connection
        .CreateChannelAsync()
        .ConfigureAwait(false);

      return _channel;
    }
    finally
    {
      _sync.Release();
    }
  }

  private void ThrowIfDisposed()
  {
    if (_disposed)
      throw new ObjectDisposedException(nameof(ModelProvider));
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed)
      return;

    _disposed = true;

    if (_channel is null)
      return;

    try
    {
      await _channel.CloseAsync().ConfigureAwait(false);
    }
    catch
    {
      // swallow on dispose
    }

    await _channel.DisposeAsync().ConfigureAwait(false);
    _channel = null;
  }

  void IDisposable.Dispose()
  {
    DisposeAsync().AsTask().GetAwaiter().GetResult();
  }
}
