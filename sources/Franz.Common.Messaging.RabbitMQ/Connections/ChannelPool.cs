#nullable enable
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public sealed class ChannelPool
  (
    IConnectionProvider connectionProvider,
    int maxRetained = 16) : IAsyncDisposable, IChannelPool
{
  private readonly ConcurrentStack<IChannel> _channels = new();
  private readonly SemaphoreSlim _createLock = new(1, 1);

  private int _count;

  public async ValueTask<IChannel> GetAsync(CancellationToken ct = default)
  {
    while (_channels.TryPop(out var channel))
    {
      Interlocked.Decrement(ref _count);

      if (channel.IsOpen)
        return channel;

      await SafeDisposeAsync(channel).ConfigureAwait(false);
    }

    return await CreateChannelAsync(ct).ConfigureAwait(false);
  }

  private async ValueTask<IChannel> CreateChannelAsync(CancellationToken ct)
  {
    await _createLock.WaitAsync(ct).ConfigureAwait(false);
    try
    {
      var connection = await connectionProvider.GetConnectionAsync(ct).ConfigureAwait(false);
      return await connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
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

    if (!channel.IsOpen)
    {
      _ = SafeDisposeAsync(channel);
      return;
    }

    while (true)
    {
      var current = Volatile.Read(ref _count);

      if (current >= maxRetained)
      {
        _ = SafeDisposeAsync(channel);
        return;
      }

      if (Interlocked.CompareExchange(ref _count, current + 1, current) == current)
      {
        _channels.Push(channel);
        return;
      }
    }
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
    catch { }
  }

  public async ValueTask DisposeAsync()
  {
    while (_channels.TryPop(out var channel))
    {
      await SafeDisposeAsync(channel).ConfigureAwait(false);
    }

    Interlocked.Exchange(ref _count, 0);
  }
}