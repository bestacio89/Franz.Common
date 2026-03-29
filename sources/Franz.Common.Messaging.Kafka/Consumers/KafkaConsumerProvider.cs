#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.KafKa.Consumers;

/// <summary>
/// Factory for Kafka Consumers. 
/// Senior Note: Implements strict validation to prevent "Ghost Consumer" initialization.
/// </summary>
public sealed class KafkaConsumerProvider(
    IOptions<KafkaMessagingOptions> messagingOptions,
    ILogger<KafkaConsumerProvider> logger)
{
  public IConsumer<Ignore, string> CreateConsumer()
  {
    var options = messagingOptions.Value;

    // --- THE ARCHITECTURAL GUARD ---
    // Prevents the native client from spinning up if the config is invalid.
    if (string.IsNullOrWhiteSpace(options.BootStrapServers))
    {
      throw new ArgumentException("Kafka BootstrapServers must be configured in MessagingOptions.", nameof(messagingOptions));
    }

    var config = new ConsumerConfig
    {
      BootstrapServers = options.BootStrapServers,
      GroupId = options.GroupID ?? $"franz-consumer-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = true,
      StatisticsIntervalMs = 5000,
      SessionTimeoutMs = 6000,
      HeartbeatIntervalMs = 2000,

      // .NET 10 / Cloud-Native Tuning
      AllowAutoCreateTopics = true,
      SocketTimeoutMs = 30000
    };

    return new ConsumerBuilder<Ignore, string>(config)
        .SetErrorHandler((_, e) =>
        {
          if (e.IsFatal)
          {
            logger.LogCritical("🚨 FATAL Kafka Error: {Reason} | Code: {Code}", e.Reason, e.Code);
          }
          else
          {
            logger.LogWarning("⚠️ Kafka Consumer Warning: {Reason}", e.Reason);
          }
        })
        .SetStatisticsHandler((_, json) =>
        {
          logger.LogDebug("📊 Kafka Statistics: {Stats}", json);
        })
        .Build();
  }
}