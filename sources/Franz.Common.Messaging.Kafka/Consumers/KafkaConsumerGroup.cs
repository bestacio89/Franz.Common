#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.KafKa.Consumers.Interfaces;

namespace Franz.Common.Messaging.KafKa.Consumers;

/// <summary>
/// Manages a persistent Kafka Consumer instance for a specific GroupID.
/// Senior Note: Uses Lazy initialization to prevent DI container failure during construction.
/// </summary>
public sealed class KafkaConsumerGroup : IConsumerGroup, IAsyncDisposable, IDisposable
{
  private readonly IConsumer<Ignore, string> _consumer;
  private bool _disposed;

  public KafkaConsumerGroup(IOptions<KafkaMessagingOptions> messagingOptions)
  {
    var options = messagingOptions.Value;

    // --- THE ARCHITECTURAL GUARD ---
    if (string.IsNullOrWhiteSpace(options.BootStrapServers))
    {
      throw new ArgumentException("Kafka BootstrapServers must be configured.", nameof(messagingOptions));
    }

    var config = new ConsumerConfig
    {
      BootstrapServers = options.BootStrapServers,
      GroupId = options.GroupID ?? $"franz-group-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = true,
      SessionTimeoutMs = 6000,
      // Optimization for .NET 10
      AllowAutoCreateTopics = true
    };

    _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
  }

  public void Subscribe(string topic)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    _consumer.Subscribe(topic);
  }

  public void Unsubscribe()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    _consumer.Unsubscribe();
  }

  public IConsumer<Ignore, string> CreateConsumer()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    return _consumer;
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed) return;

    // Senior Note: We must Close() before Dispose() to ensure the group coordinator 
    // is notified and rebalance is triggered immediately for other members.
    await Task.Run(() =>
    {
      _consumer.Close();
      _consumer.Dispose();
    }).ConfigureAwait(false);

    _disposed = true;
    GC.SuppressFinalize(this);
  }

  public void Dispose()
  {
    if (_disposed) return;
    _consumer.Close();
    _consumer.Dispose();
    _disposed = true;
  }
}