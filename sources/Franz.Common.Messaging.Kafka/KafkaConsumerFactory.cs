#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Kafka.Validation;

namespace Franz.Common.Messaging.Kafka;

public sealed class KafkaConsumerFactory : IKafkaConsumerFactory
{
  private readonly KafkaMessagingOptions _options;
  private readonly ILogger<KafkaConsumerFactory> _logger;
  private readonly KafkaRebalanceCoordinator _rebalanceCoordinator;

  public KafkaConsumerFactory(
      IOptions<KafkaMessagingOptions> options,
      ILogger<KafkaConsumerFactory> logger,
      KafkaRebalanceCoordinator rebalanceCoordinator)
  {
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _rebalanceCoordinator = rebalanceCoordinator ?? throw new ArgumentNullException(nameof(rebalanceCoordinator));
  }

  public IConsumer<string, string> Build()
  {
    var validation = new KafkaMessagingOptionsValidator().Validate(Options.DefaultName, _options);
    if (validation.Failed)
      throw new OptionsValidationException(Options.DefaultName, typeof(KafkaMessagingOptions), validation.Failures);

    var options = _options.Consumer;
    if (options.EnableAutoCommit || options.EnableAutoOffsetStore)
    {
      throw new InvalidOperationException(
          "Franz Kafka consumers commit offsets only after successful handler completion. " +
          "EnableAutoCommit and EnableAutoOffsetStore must both be false.");
    }

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
      SslKeyLocation = _options.Security.SslKeyLocation,
      AllowAutoCreateTopics = false,
      PartitionAssignmentStrategy = _options.Consumer.EnableCooperativeStickyAssignor
          ? PartitionAssignmentStrategy.CooperativeSticky
          : PartitionAssignmentStrategy.Range
    };

    var groupId = _options.GroupId ?? throw new InvalidOperationException("Kafka GroupId is required.");
    var registration = _rebalanceCoordinator.CreateRegistration(groupId);

    IConsumer<string, string>? consumer = null;
    try
    {
      consumer = new ConsumerBuilder<string, string>(config)
          .SetPartitionsAssignedHandler((client, partitions) =>
          {
            _rebalanceCoordinator.OnPartitionsAssigned(
                registration, partitions);

            if (_rebalanceCoordinator.IsHandlerExecutionActive(registration) && partitions.Count > 0)
            {
              try { client.Pause(partitions); }
              catch (KafkaException ex)
              {
                _logger.LogWarning(ex,
                    "Could not pause newly assigned Kafka partitions for consumer {ConsumerId} while a handler is in flight.",
                    registration.Id);
              }
            }
          })
          .SetPartitionsRevokedHandler((_, partitions) =>
          {
            if (partitions.Count == 0)
              return;

            _rebalanceCoordinator.OnPartitionsRevoked(
                registration, partitions.Select(static partition => partition.TopicPartition));

            _logger.LogInformation(
                "Kafka partitions revoked for consumer {ConsumerId} in group {GroupId}: {Partitions}.",
                registration.Id,
                groupId,
                string.Join(", ", partitions.Select(partition => $"{partition.Topic}[{partition.Partition.Value}]@{partition.Offset.Value}")));
          })
          .SetPartitionsLostHandler((_, partitions) =>
          {
            if (partitions.Count == 0)
              return;

            _rebalanceCoordinator.OnPartitionsLost(
                registration, partitions.Select(static partition => partition.TopicPartition));

            _logger.LogWarning(
                "Kafka partitions lost for consumer {ConsumerId} in group {GroupId}: {Partitions}. Offsets may be redelivered; handlers must be idempotent.",
                registration.Id,
                groupId,
                string.Join(", ", partitions.Select(partition => $"{partition.Topic}[{partition.Partition.Value}]@{partition.Offset.Value}")));
          })
          .SetErrorHandler((_, error) =>
          {
            if (error.IsFatal)
              _logger.LogCritical("Fatal Kafka error: {Reason} | Local: {IsLocal}", error.Reason, error.IsLocalError);
            else
              _logger.LogWarning("Kafka warning: {Reason}", error.Reason);
          })
          .SetStatisticsHandler((_, json) => _logger.LogTrace("Kafka statistics: {Stats}", json))
          .SetLogHandler((_, message) =>
              _logger.LogDebug("Kafka internal log: [{Level}] {Message}", message.Level, message.Message))
          .Build();

      _rebalanceCoordinator.RegisterConsumer(consumer, registration);
      return consumer;
    }
    catch
    {
      consumer?.Dispose();
      _rebalanceCoordinator.ReleaseRegistration(registration);
      throw;
    }
  }
}