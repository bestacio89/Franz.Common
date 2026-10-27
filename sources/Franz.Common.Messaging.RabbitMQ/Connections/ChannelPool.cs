#nullable enable
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

/// <summary>
/// Reuses idle RabbitMQ channels while preserving explicit pool shutdown semantics.
/// </summary>
public sealed class ChannelPool : IAsyncDisposable, IChannelPool
{
  private readonly IConnectionProvider _connectionProvider;
  private readonly int _maxRetained;
  private readonly ConcurrentStack<IChannel> _channels = new();
  private readonly SemaphoreSlim _createLock = new(1, 1);
  private readonly object _gate = new();
  private int _count;
  private bool _disposed;

  public ChannelPool(IConnectionProvider connectionProvider, int maxRetained = 16)
  {
    _connectionProvider = connectionProvider
        ?? throw new ArgumentNullException(nameof(connectionProvider));
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetained);
    _maxRetained = maxRetained;
  }

  public async ValueTask<IChannel> GetAsync(CancellationToken ct = default)
  {
    while (true)
    {
      IChannel? channel;
      lock (_gate)
      {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_channels.TryPop(out channel))
          channel = null;
        else
          _count--;
      }

      if (channel is null)
        return await CreateChannelAsync(ct).ConfigureAwait(false);

      if (channel.IsOpen)
        return channel;

      await SafeDisposeAsync(channel).ConfigureAwait(false);
    }
  }

  private async ValueTask<IChannel> CreateChannelAsync(CancellationToken ct)
  {
    await _createLock.WaitAsync(ct).ConfigureAwait(false);
    try
    {
      lock (_gate)
        ObjectDisposedException.ThrowIf(_disposed, this);

      var connection = await _connectionProvider.GetConnectionAsync(ct).ConfigureAwait(false);
      var channel = await connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

      lock (_gate)
      {
        if (!_disposed)
          return channel;
      }

      await SafeDisposeAsync(channel).ConfigureAwait(false);
      throw new ObjectDisposedException(nameof(ChannelPool));
    }
    finally
    {
      _createLock.Release();
    }
  }

  public void Return(IChannel? channel)
  {
    if (channel is null)
      return;

    var dispose = !channel.IsOpen;
    if (!dispose)
    {
      lock (_gate)
      {
        if (_disposed || _count >= _maxRetained)
        {
          dispose = true;
        }
        else
        {
          _channels.Push(channel);
          _count++;
        }
      }
    }

    if (dispose)
      _ = SafeDisposeAsync(channel);
  }

  private static async ValueTask SafeDisposeAsync(IChannel channel)
  {
    try
    {
      if (channel.IsOpen)
      {
        try { await channel.CloseAsync().ConfigureAwait(false); }
        catch { }
      }

      await channel.DisposeAsync().ConfigureAwait(false);
    }
    catch
    {
      // Pool cleanup must not mask the transport failure that triggered disposal.
    }
  }

  public async ValueTask DisposeAsync()
  {
    List<IChannel> channelsToDispose = new();

    lock (_gate)
    {
      if (_disposed)
        return;

      _disposed = true;
      while (_channels.TryPop(out var channel))
        channelsToDispose.Add(channel);
      _count = 0;
    }

    // Wait for any in-flight channel creation to observe the disposed state.
    await _createLock.WaitAsync().ConfigureAwait(false);
    _createLock.Release();

    foreach (var channel in channelsToDispose)
      await SafeDisposeAsync(channel).ConfigureAwait(false);
  }
}