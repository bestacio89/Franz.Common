#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.KafKa.Consumers.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Threading;

namespace Franz.Common.Messaging.KafKa.Consumers;

/// <summary>
/// Represents a Kafka consumer group with proper disposal and fallback group ID logic.
/// </summary>
public sealed class KafkaConsumerGroup : IConsumerGroup, IAsyncDisposable, IDisposable
{
  private readonly IConsumer<Ignore, string> _consumer;
  private bool _disposed;

  /// <summary>
  /// The effective GroupId used by this consumer group.
  /// </summary>
  public string GroupId { get; }

  public KafkaConsumerGroup(IOptions<KafkaMessagingOptions> messagingOptions)
  {
    var options = messagingOptions?.Value ?? throw new ArgumentNullException(nameof(messagingOptions));

    if (string.IsNullOrWhiteSpace(options.BootstrapServers))
      throw new ArgumentException("Kafka BootstrapServers must be configured.", nameof(messagingOptions));

    var consumerOptions = options.Consumer;

    // Compute GroupId: fallback if null or empty
    GroupId = string.IsNullOrWhiteSpace(options.GroupId)
        ? $"franz-group-{Guid.NewGuid():N}"
        : options.GroupId;

    var config = new ConsumerConfig
    {
      BootstrapServers = options.BootstrapServers,
      GroupId = GroupId,
      AutoOffsetReset = consumerOptions.AutoOffsetReset switch
      {
        KafkaAutoOffsetReset.Latest => AutoOffsetReset.Latest,
        KafkaAutoOffsetReset.None => AutoOffsetReset.Error,
        _ => AutoOffsetReset.Earliest
      },
      EnableAutoCommit = consumerOptions.EnableAutoCommit,
      SessionTimeoutMs = consumerOptions.SessionTimeoutMs,
      MaxPollIntervalMs = consumerOptions.MaxPollIntervalMs,
      FetchMaxBytes = consumerOptions.FetchMaxBytes,
      AllowAutoCreateTopics = true
    };

    _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
  }

  /// <summary>
  /// Subscribes to a Kafka topic.
  /// </summary>
  /// <param name="topic">The topic name.</param>
  public void Subscribe(string topic)
  {
    ThrowIfDisposed();
    _consumer.Subscribe(topic);
  }

  /// <summary>
  /// Unsubscribes from all topics.
  /// </summary>
  public void Unsubscribe()
  {
    ThrowIfDisposed();
    _consumer.Unsubscribe();
  }

  /// <summary>
  /// Returns the underlying Kafka consumer instance.
  /// </summary>
  public IConsumer<Ignore, string> CreateConsumer()
  {
    ThrowIfDisposed();
    return _consumer;
  }

  /// <summary>
  /// Synchronously disposes the consumer and closes the connection.
  /// </summary>
  public void Dispose()
  {
    if (_disposed) return;

    try
    {
      _consumer.Close();
    }
    catch
    {
      // Swallow exceptions on dispose
    }

    _consumer.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Asynchronously disposes the consumer.
  /// </summary>
  public ValueTask DisposeAsync()
  {
    if (_disposed) return ValueTask.CompletedTask;

    try
    {
      _consumer.Close();
    }
    catch
    {
      // Swallow exceptions on dispose
    }

    _consumer.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }

  private void ThrowIfDisposed()
  {
    if (_disposed)
      throw new ObjectDisposedException(nameof(KafkaConsumerGroup));
  }
}