#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

/// <summary>
/// Owns one recoverable RabbitMQ connection for the application service provider.
/// </summary>
public sealed class ConnectionProvider : IConnectionProvider, IAsyncDisposable
{
  private readonly IConnectionFactoryProvider _factoryProvider;
  private readonly SemaphoreSlim _lock = new(1, 1);
  private IConnection? _connection;
  private volatile bool _disposed;

  public ConnectionProvider(IConnectionFactoryProvider factoryProvider)
  {
    _factoryProvider = factoryProvider
        ?? throw new ArgumentNullException(nameof(factoryProvider));
  }

  public async ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      ObjectDisposedException.ThrowIf(_disposed, this);

      if (_connection is { IsOpen: true })
        return _connection;

      var factory = _factoryProvider.Current;
      var newConnection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

      if (_disposed)
      {
        newConnection.Dispose();
        throw new ObjectDisposedException(nameof(ConnectionProvider));
      }

      var previousConnection = _connection;
      _connection = newConnection;

      if (previousConnection is not null)
      {
        try { previousConnection.Dispose(); }
        catch { /* A stale connection must not prevent replacement. */ }
      }

      return newConnection;
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
          // Preserve deterministic disposal even when the broker is already unavailable.
        }

        _connection.Dispose();
        _connection = null;
      }
    }
    finally
    {
      _lock.Release();
    }
  }
}