using System.Threading;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public sealed class ConnectionProvider : IConnectionProvider, IDisposable
{
  private readonly IConnectionFactoryProvider _factoryProvider;
  private readonly SemaphoreSlim _sync = new(1, 1);

  private IConnection? _connection;
  private bool _disposed;

  public ConnectionProvider(IConnectionFactoryProvider factoryProvider)
  {
    _factoryProvider = factoryProvider;
  }

  public IConnection Current
  {
    get
    {
      ThrowIfDisposed();
      return GetOrCreateConnection();
    }
  }

  private IConnection GetOrCreateConnection()
  {
    if (_connection is { IsOpen: true })
      return _connection;

    // Fast path failed → secure creation via lock
    _sync.Wait();
    try
    {
      if (_connection is { IsOpen: true })
        return _connection;

      var factory = _factoryProvider.Current;
      _connection = (IConnection?)factory.CreateConnectionAsync();

      return _connection;
    }
    finally
    {
      _sync.Release();
    }
  }

  private void ThrowIfDisposed()
  {
    if (_disposed)
      throw new ObjectDisposedException(nameof(ConnectionProvider));
  }

  public void Dispose()
  {
    if (_disposed)
      return;

    _disposed = true;

    try
    {
      _connection?.Dispose();
    }
    catch
    {
      // swallow — closing a dead connection may throw
    }

    _connection?.Dispose();
    _sync.Dispose();
  }
}
