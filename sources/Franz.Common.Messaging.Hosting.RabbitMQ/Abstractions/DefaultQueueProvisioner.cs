#nullable enable
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Franz.Common.Messaging.RabbitMQ.Modeling;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Abstractions;

public sealed class DefaultQueueProvisioner : IQueueProvisioner
{
  /// <summary>
  /// Ensures the specified queue exists using the provided model provider.
  /// </summary>
  public async Task EnsureQueueExistsAsync(
      IModelProvider modelProvider,
      string queueName,
      CancellationToken ct = default)
  {
    // 1. Acquire the IChannel asynchronously from the provider
    // Note: We do not 'using' the channel here because the IModelProvider 
    // usually manages the long-running lifecycle of the underlying channel/connection.
    var channel = await modelProvider.GetChannelAsync(ct);

    // 2. Execute the asynchronous queue declaration
    await channel.QueueDeclareAsync(
        queue: queueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null,
        cancellationToken: ct);
  }
}