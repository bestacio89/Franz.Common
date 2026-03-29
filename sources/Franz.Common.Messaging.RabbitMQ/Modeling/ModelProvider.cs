using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Messaging.RabbitMQ.Connections;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public sealed class ModelProvider : IModelProvider, IAsyncDisposable
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IChannel? _channel;
    private bool _disposed;

    public ModelProvider(IConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_channel is { IsOpen: true })
            return _channel;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            _channel = await connection.CreateChannelAsync().ConfigureAwait(false);

            return _channel;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync().ConfigureAwait(false);
                _channel.Dispose();
                _channel = null;
            }
        }
        finally
        {
            _semaphore.Release();
            _semaphore.Dispose();
        }
    }
}
