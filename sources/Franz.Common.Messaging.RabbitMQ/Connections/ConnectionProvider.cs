#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public sealed class ConnectionProvider : IConnectionProvider, IAsyncDisposable
{
  private readonly IConnectionFactoryProvider _factoryProvider;
  private readonly SemaphoreSlim _lock = new(1, 1);

  private IConnection? _connection;
  private bool _disposed;

  public ConnectionProvider(IConnectionFactoryProvider factoryProvider)
  {
    _factoryProvider = factoryProvider;
  }

  public async ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    // Fast path (no lock)
    if (_connection is { IsOpen: true })
      return _connection;

    await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      // Double-check after acquiring lock
      if (_connection is { IsOpen: true })
        return _connection;

      var factory = _factoryProvider.Current;

      _connection = await factory
          .CreateConnectionAsync(cancellationToken)
          .ConfigureAwait(false);

      return _connection;
    }
    finally
    {
      _lock.Release();
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed)
      return;

    _disposed = true;

    await _lock.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_connection is not null)
      {
        try
        {
          if (_connection.IsOpen)
            await _connection.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
          // swallow — shutdown path
        }

        _connection.Dispose();
        _connection = null;
      }
    }
    finally
    {
      _lock.Release();
      _lock.Dispose();
    }
  }
}