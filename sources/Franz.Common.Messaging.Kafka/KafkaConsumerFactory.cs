#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka;

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
    var options = _options.Consumer;
    var config = new ConsumerConfig
    {
      BootstrapServers = _options.BootstrapServers,
      GroupId = _options.GroupId,
      AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(
          options.AutoOffsetReset.ToString(), true, out var reset)
          ? reset
          : AutoOffsetReset.Earliest,
      EnableAutoCommit = options.EnableAutoCommit,
      EnableAutoOffsetStore = options.EnableAutoOffsetStore,
      SessionTimeoutMs = options.SessionTimeoutMs,
      MaxPollIntervalMs = options.MaxPollIntervalMs,
      MaxPollRecords = options.MaxPollRecords,
      FetchMinBytes = options.FetchMinBytes,
      FetchMaxBytes = options.FetchMaxBytes,
      FetchWaitMaxMs = options.FetchWaitMaxMs,
      SecurityProtocol = Enum.TryParse<SecurityProtocol>(
          _options.Security.SecurityProtocol.ToString(), true, out var securityProtocol)
          ? securityProtocol
          : SecurityProtocol.Plaintext,
      SaslMechanism = Enum.TryParse<SaslMechanism>(
          _options.Security.SaslMechanism?.ToString(), true, out var saslMechanism)
          ? saslMechanism
          : null,
      SaslUsername = _options.Security.SaslUsername,
      SaslPassword = _options.Security.SaslPassword,
      SslCaLocation = _options.Security.SslCaLocation,
      SslCertificateLocation = _options.Security.SslCertificateLocation,
      SslKeyLocation = _options.Security.SslKeyLocation
    };

    return new ConsumerBuilder<string, string>(config)
        .SetErrorHandler((_, error) =>
        {
          if (error.IsFatal)
            _logger.LogCritical("Fatal Kafka error: {Reason} | Local: {IsLocal}",
                error.Reason, error.IsLocalError);
          else
            _logger.LogWarning("Kafka warning: {Reason}", error.Reason);
        })
        .SetStatisticsHandler((_, json) => _logger.LogTrace("Kafka statistics: {Stats}", json))
        .SetLogHandler((_, message) =>
            _logger.LogDebug("Kafka internal log: [{Level}] {Message}", message.Level, message.Message))
        .Build();
  }
}
