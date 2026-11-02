#nullable enable
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

/// <summary>
/// Reuses idle RabbitMQ channels while preserving explicit pool shutdown semantics.
/// </summary>
public sealed class ChannelPool : IAsyncDisposable, IChannelPool, IConfirmChannelPool
{
  private readonly IConnectionProvider _connectionProvider;
  private readonly int _maxRetained;
  private readonly ConcurrentStack<IChannel> _channels = new();
  private readonly ConcurrentStack<IChannel> _publisherConfirmChannels = new();
  private readonly SemaphoreSlim _createLock = new(1, 1);
  private readonly object _gate = new();
  private int _count;
  private int _publisherConfirmCount;
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

  public async ValueTask<IChannel> GetPublisherConfirmChannelAsync(CancellationToken cancellationToken = default)
  {
    while (true)
    {
      IChannel? channel;
      lock (_gate)
      {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_publisherConfirmChannels.TryPop(out channel))
          channel = null;
        else
          _publisherConfirmCount--;
      }

      if (channel is null)
        return await CreatePublisherConfirmChannelAsync(cancellationToken).ConfigureAwait(false);

      if (channel.IsOpen)
        return channel;

      await SafeDisposeAsync(channel).ConfigureAwait(false);
    }
  }

  public void ReturnPublisherConfirmChannel(IChannel? channel)
  {
    if (channel is null)
      return;

    var dispose = !channel.IsOpen;
    if (!dispose)
    {
      lock (_gate)
      {
        if (_disposed || _publisherConfirmCount >= _maxRetained)
          dispose = true;
        else
        {
          _publisherConfirmChannels.Push(channel);
          _publisherConfirmCount++;
        }
      }
    }

    if (dispose)
      _ = SafeDisposeAsync(channel);
  }

  private async ValueTask<IChannel> CreatePublisherConfirmChannelAsync(CancellationToken cancellationToken)
  {
    await _createLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      lock (_gate)
        ObjectDisposedException.ThrowIf(_disposed, this);

      var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
      var channel = await connection.CreateChannelAsync(
          new CreateChannelOptions(
              publisherConfirmationsEnabled: true,
              publisherConfirmationTrackingEnabled: true),
          cancellationToken: cancellationToken).ConfigureAwait(false);

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

  public async ValueTask<IChannel> GetTransactionalChannelAsync(CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    var connection = await _connectionProvider.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
    return await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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
      while (_publisherConfirmChannels.TryPop(out var confirmChannel))
        channelsToDispose.Add(confirmChannel);
      _count = 0;
      _publisherConfirmCount = 0;
    }

    // Wait for any in-flight channel creation to observe the disposed state.
    await _createLock.WaitAsync().ConfigureAwait(false);
    _createLock.Release();

    foreach (var channel in channelsToDispose)
      await SafeDisposeAsync(channel).ConfigureAwait(false);
  }
}