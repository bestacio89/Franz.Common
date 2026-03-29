using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

/// <summary>
/// Defines a thread-safe pool for RabbitMQ v7+ IChannel instances.
/// Senior Note: Aligned with concrete ChannelPool to support efficient 
/// ValueTask hot-paths and automatic resource reclamation.
/// </summary>
public interface IChannelPool : IAsyncDisposable
{
  /// <summary>
  /// Retrieves an open channel from the pool or creates a new one.
  /// </summary>
  ValueTask<IChannel> GetAsync(CancellationToken ct = default);

  /// <summary>
  /// Returns a channel to the pool. If the pool is full or the channel 
  /// is closed, it will be safely disposed.
  /// </summary>
  void Return(IChannel? channel);
}