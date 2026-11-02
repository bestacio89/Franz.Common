using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

/// <summary>
/// Provides isolated channels for confirmed publishing and transactional publishing.
/// These channels are never returned to the general-purpose channel pool.
/// </summary>
public interface IConfirmChannelPool
{
  ValueTask<IChannel> GetPublisherConfirmChannelAsync(CancellationToken cancellationToken = default);

  ValueTask<IChannel> GetTransactionalChannelAsync(CancellationToken cancellationToken = default);
}
