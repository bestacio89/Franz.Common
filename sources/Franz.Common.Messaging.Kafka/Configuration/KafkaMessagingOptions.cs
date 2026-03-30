#nullable enable
using System.ComponentModel.DataAnnotations;

namespace Franz.Common.Messaging.Kafka.Configuration;

public class KafkaMessagingOptions
{
  public const string SectionName = "Messaging:Kafka";

  [Required(AllowEmptyStrings = false)]
  public required string BootstrapServers { get; set; }
  public string? GroupId { get; set; }
  public string? ClientId { get; set; }

  public string? TopicName{ get; set; }

  public KafkaSecurityOptions Security { get; set; } = new();

  [Required]
  public KafkaConsumerOptions Consumer { get; set; } = new();

  [Required]
  public KafkaProducerOptions Producer { get; set; } = new();

  public KafkaFailureOptions Failure { get; set; } = new();

  public KafkaObservabilityOptions Observability { get; set; } = new();
}

public class KafkaSecurityOptions
{
  public KafkaSecurityProtocol SecurityProtocol { get; set; } = KafkaSecurityProtocol.Plaintext;

  public KafkaSaslMechanism? SaslMechanism { get; set; }

  public string? SaslUsername { get; set; }

  public string? SaslPassword { get; set; }

  public string? SslCaLocation { get; set; }

  public string? SslCertificateLocation { get; set; }

  public string? SslKeyLocation { get; set; }
}

public enum KafkaSecurityProtocol
{
  Plaintext,
  Ssl,
  SaslPlaintext,
  SaslSsl
}

public enum KafkaSaslMechanism
{
  Plain,
  ScramSha256,
  ScramSha512,
  Gssapi,
  OAuthBearer
}

public class KafkaConsumerOptions
{
  [Required(AllowEmptyStrings = false)]
  

  public KafkaAutoOffsetReset AutoOffsetReset { get; set; } = KafkaAutoOffsetReset.Earliest;

  public bool EnableAutoCommit { get; set; } = false;

  public bool EnableAutoOffsetStore { get; set; } = false;

  [Range(1000, 300000)]
  public int SessionTimeoutMs { get; set; } = 45000;

  [Range(1000, 3000000)]
  public int MaxPollIntervalMs { get; set; } = 300000;

  public int MaxPollRecords { get; set; } = 500;

  public int FetchMinBytes { get; set; } = 1;

  public int FetchMaxBytes { get; set; } = 52428800;

  public int FetchWaitMaxMs { get; set; } = 500;

  public int ConcurrencyLevel { get; set; } = 1; // Parallel processing control
}

public enum KafkaAutoOffsetReset
{
  Earliest,
  Latest,
  None
}

public class KafkaProducerOptions
{
  public KafkaAcks Acks { get; set; } = KafkaAcks.All;

  [Range(1024, int.MaxValue)]
  public int MessageMaxBytes { get; set; } = 1048576;

  public int LingerMs { get; set; } = 5;

  public int BatchSize { get; set; } = 16384;

  public KafkaCompressionType CompressionType { get; set; } = KafkaCompressionType.Snappy;

  public bool EnableIdempotence { get; set; } = true;

  // 🔁 Reliability
  public int MessageSendMaxRetries { get; set; } = 3;

  public int RetryBackoffMs { get; set; } = 100;

  public int DeliveryTimeoutMs { get; set; } = 120000;

  // 🔑 Partitioning
  public string? DefaultKey { get; set; }

}

public enum KafkaAcks
{
  None,
  Leader,
  All
}

public enum KafkaCompressionType
{
  None,
  Gzip,
  Snappy,
  Lz4,
  Zstd
}


public class KafkaFailureOptions
{
  public bool EnableDeadLetter { get; set; } = true;

  public string? DeadLetterTopic { get; set; }

  public int MaxRetryAttempts { get; set; } = 3;

  public int RetryBackoffMs { get; set; } = 500;

  public bool UseOriginalMessageKey { get; set; } = true;
}

public class KafkaObservabilityOptions
{
  public bool EnableMetrics { get; set; } = true;

  public bool EnableTracing { get; set; } = true;

  public bool EnableLogging { get; set; } = true;

  public bool LogMessagePayloads { get; set; } = false; // avoid PII explosion
}