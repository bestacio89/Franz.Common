#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Factory for generating pre-configured Kafka consumers.
/// Configured for 'Earliest' offset reset to prevent data loss on new group initialization.
/// </summary>
public sealed class KafkaConsumerFactory : IKafkaConsumerFactory
{
  private readonly KafkaMessagingOptions _options;
  private readonly ILogger<KafkaConsumerFactory> _logger;

  public KafkaConsumerFactory(
      IOptions<KafkaMessagingOptions> options,
      ILogger<KafkaConsumerFactory> logger)
  {
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public IConsumer<string, string> Build()
  {
    // --- Guard Clauses: fail fast ---
    ArgumentException.ThrowIfNullOrWhiteSpace(_options.BootStrapServers, nameof(_options.BootStrapServers));
    ArgumentException.ThrowIfNullOrWhiteSpace(_options.GroupID, nameof(_options.GroupID));

    var config = new ConsumerConfig
    {
      BootstrapServers = _options.BootStrapServers,
      GroupId = _options.GroupID,
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = true,
      AutoCommitIntervalMs = 5000,
      StatisticsIntervalMs = 10000,
      SessionTimeoutMs = 6000,
      HeartbeatIntervalMs = 2000,
      FetchWaitMaxMs = 100,
      MaxPartitionFetchBytes = 1_048_576 // 1MB per partition fetch
    };

    return new ConsumerBuilder<string, string>(config)
        .SetErrorHandler((_, e) =>
        {
          if (e.IsFatal)
            _logger.LogCritical("🚨 FATAL Kafka Error: {Reason} | Local: {IsLocal}", e.Reason, e.IsLocalError);
          else
            _logger.LogWarning("⚠️ Kafka Warning: {Reason}", e.Reason);
        })
        .SetStatisticsHandler((_, json) =>
        {
          _logger.LogTrace("📊 Kafka Stats: {Stats}", json);
        })
        .SetLogHandler((_, msg) =>
        {
          _logger.LogDebug("📝 Kafka Internal: [{Level}] {Message}", msg.Level, msg.Message);
        })
        .Build();
  }
}