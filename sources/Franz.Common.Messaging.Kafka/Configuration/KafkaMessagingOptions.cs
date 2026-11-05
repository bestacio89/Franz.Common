#nullable enable
using System.ComponentModel.DataAnnotations;
using Franz.Common.Messaging.Configuration;

namespace Franz.Common.Messaging.Kafka.Configuration;

public class KafkaMessagingOptions
{
  public const string SectionName = "Messaging:Kafka";

  [Required(AllowEmptyStrings = false)]
  public required string BootstrapServers { get; set; }

  [Required(AllowEmptyStrings = false)]
  public string? GroupId { get; set; }

  public string? ClientId { get; set; }
  public string? TopicName { get; set; }
  public KafkaSecurityOptions Security { get; set; } = new();
  [Required] public KafkaConsumerOptions Consumer { get; set; } = new();
  [Required] public KafkaProducerOptions Producer { get; set; } = new();
  public KafkaObservabilityOptions Observability { get; set; } = new();
  public KafkaTopicOptions Topic { get; set; } = new();

  /// <summary>Optional topic specification applied to all dead-letter topics.</summary>
  public KafkaTopicOptions? DeadLetterTopicOptions { get; set; }

  /// <summary>Optional topic specifications keyed by generated source topic name.</summary>
  public Dictionary<string, KafkaTopicOptions> EventTopicOptions { get; set; } = new(StringComparer.Ordinal);
  public ProvisioningMode ProvisioningMode { get; set; } = ProvisioningMode.Create;
  public string? DeadLetterTopic { get; set; }
  public MessageFailurePolicy FailurePolicy { get; set; } = MessageFailurePolicy.Default;
  public MessageOrderingStrategy OrderingStrategy { get; set; } = MessageOrderingStrategy.MessageId;
}

public class KafkaSecurityOptions
{
  public KafkaSecurityProtocol SecurityProtocol { get; set; } = KafkaSecurityProtocol.Plaintext;

  /// <summary>Explicitly permits unencrypted Kafka transport. Keep disabled outside isolated development and test environments.</summary>
  public bool AllowInsecureTransport { get; set; }
  public KafkaSaslMechanism? SaslMechanism { get; set; }
  public string? SaslUsername { get; set; }
  public string? SaslPassword { get; set; }
  public string? SslCaLocation { get; set; }
  public string? SslCertificateLocation { get; set; }
  public string? SslKeyLocation { get; set; }
}

public enum KafkaSecurityProtocol { Plaintext, Ssl, SaslPlaintext, SaslSsl }
public enum KafkaSaslMechanism { Plain, ScramSha256, ScramSha512, Gssapi, OAuthBearer }

public class KafkaConsumerOptions
{
  public KafkaAutoOffsetReset AutoOffsetReset { get; set; } = KafkaAutoOffsetReset.Earliest;
  public bool EnableAutoCommit { get; set; } = false;
  public bool EnableAutoOffsetStore { get; set; } = false;

  [Range(1000, 300000)]
  public int SessionTimeoutMs { get; set; } = 45000;

  [Range(1000, 3000000)]
  public int MaxPollIntervalMs { get; set; } = 300000;

  [Range(1, 10000)]
  public int MaxPollRecords { get; set; } = 500;

  /// <summary>Opt in to Kafka's cooperative-sticky partition assignor.</summary>
  public bool EnableCooperativeStickyAssignor { get; set; } = false;

  [Range(1, int.MaxValue)]
  public int FetchMinBytes { get; set; } = 1;

  [Range(1024, int.MaxValue)]
  public int FetchMaxBytes { get; set; } = 52428800;

  [Range(0, 60000)]
  public int FetchWaitMaxMs { get; set; } = 500;

  [Range(1, 100)]
  public int ConcurrencyLevel { get; set; } = 1;
}

public enum KafkaAutoOffsetReset { Earliest, Latest, None }

public class KafkaProducerOptions
{
  public KafkaAcks Acks { get; set; } = KafkaAcks.All;

  [Range(1024, int.MaxValue)]
  public int MessageMaxBytes { get; set; } = 1048576;

  [Range(0, 60000)]
  public int LingerMs { get; set; } = 5;

  [Range(1, int.MaxValue)]
  public int BatchSize { get; set; } = 16384;

  public KafkaCompressionType CompressionType { get; set; } = KafkaCompressionType.Snappy;
  public bool EnableIdempotence { get; set; } = true;

  /// <summary>Optional prefix for unique transaction producer identities. A unique suffix is generated per transaction.</summary>
  public string? TransactionalIdPrefix { get; set; }

  [Range(0, 1000)]
  public int MessageSendMaxRetries { get; set; } = 3;

  [Range(0, 60000)]
  public int RetryBackoffMs { get; set; } = 100;

  [Range(1, 900000)]
  public int DeliveryTimeoutMs { get; set; } = 120000;

  /// <summary>Maximum duration of a Kafka transaction, in milliseconds. Must be at least the producer delivery timeout.</summary>
  [Range(1, 900000)]
  public int TransactionTimeoutMs { get; set; } = 120000;

}

public enum KafkaAcks { None, Leader, All }
public enum KafkaCompressionType { None, Gzip, Snappy, Lz4, Zstd }

public class KafkaObservabilityOptions
{
  public bool EnableMetrics { get; set; } = true;
  public bool EnableTracing { get; set; } = true;
  public bool EnableLogging { get; set; } = true;
  public bool LogMessagePayloads { get; set; } = false;
}
public class KafkaTopicOptions
{
  [Range(1, 10000)]
  public int NumPartitions { get; set; } = 1;

  [Range(1, short.MaxValue)]
  public short ReplicationFactor { get; set; } = 1;

  [Range(100, 300000)]
  public int CreationTimeoutMs { get; set; } = 30000;

  /// <summary>Topic configuration keys and values passed to Kafka topic creation.</summary>
  public Dictionary<string, string> Configurations { get; set; } = new(StringComparer.Ordinal);
}
