#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Factory for generating pre-configured Kafka consumers.
/// Driven by strongly-typed, host-validated KafkaMessagingOptions.
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
    var config = new ConsumerConfig
    {
      BootstrapServers = _options.BootstrapServers,
      GroupId = _options.GroupId,
      AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_options.Consumer.AutoOffsetReset.ToString(), true, out var reset) ? reset : AutoOffsetReset.Earliest,
      EnableAutoCommit = _options.Consumer.EnableAutoCommit,
      EnableAutoOffsetStore = _options.Consumer.EnableAutoOffsetStore,
      SessionTimeoutMs = _options.Consumer.SessionTimeoutMs,
      MaxPollIntervalMs = _options.Consumer.MaxPollIntervalMs,
      FetchMaxBytes = _options.Consumer.FetchMaxBytes,

      // Security Mapping
      SecurityProtocol = Enum.TryParse<SecurityProtocol>(_options.Security.SecurityProtocol.ToString(), true, out var sec) ? sec : SecurityProtocol.Plaintext,
      SaslMechanism = Enum.TryParse<SaslMechanism>(_options.Security.SaslMechanism.ToString(), true, out var sasl) ? sasl : null,
      SaslUsername = _options.Security.SaslUsername,
      SaslPassword = _options.Security.SaslPassword,
      SslCaLocation = _options.Security.SslCaLocation,
      SslCertificateLocation = _options.Security.SslCertificateLocation,
      SslKeyLocation = _options.Security.SslKeyLocation
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