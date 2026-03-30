using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Connections;
using Microsoft.Extensions.Options;

public sealed class ConnectionFactoryProvider : IConnectionFactoryProvider
{
  private readonly KafkaMessagingOptions _options;

  public ConnectionFactoryProvider(IOptions<KafkaMessagingOptions> messagingOptions)
  {
    _options = messagingOptions?.Value ?? throw new ArgumentNullException(nameof(messagingOptions));
  }

  public ProducerConfig Current => BuildProducerConfig();

  private ProducerConfig BuildProducerConfig()
  {
    return new ProducerConfig
    {
      BootstrapServers = _options.BootstrapServers,
      ClientId = _options.ClientId,
      Acks = MapAcks(_options.Producer.Acks),
      EnableIdempotence = _options.Producer.EnableIdempotence,
      MessageMaxBytes = _options.Producer.MessageMaxBytes,
      LingerMs = _options.Producer.LingerMs,
      BatchSize = _options.Producer.BatchSize,
      CompressionType = MapCompression(_options.Producer.CompressionType),

      // Security
      SecurityProtocol = MapSecurityProtocol(_options.Security.SecurityProtocol),
      SaslMechanism = MapSaslMechanism(_options.Security.SaslMechanism),
      SaslUsername = _options.Security.SaslUsername,
      SaslPassword = _options.Security.SaslPassword,
      SslCaLocation = _options.Security.SslCaLocation,
      SslCertificateLocation = _options.Security.SslCertificateLocation,
      SslKeyLocation = _options.Security.SslKeyLocation,
    };
  }

  // --- Mappers ---
  private static Acks MapAcks(KafkaAcks a) => a switch
  {
    KafkaAcks.None => Acks.None,
    KafkaAcks.Leader => Acks.Leader,
    _ => Acks.All
  };

  private static CompressionType MapCompression(KafkaCompressionType c) => c switch
  {
    KafkaCompressionType.Gzip => CompressionType.Gzip,
    KafkaCompressionType.Lz4 => CompressionType.Lz4,
    KafkaCompressionType.Zstd => CompressionType.Zstd,
    KafkaCompressionType.None => CompressionType.None,
    _ => CompressionType.Snappy
  };

  private static SecurityProtocol MapSecurityProtocol(KafkaSecurityProtocol p) => p switch
  {
    KafkaSecurityProtocol.Ssl => SecurityProtocol.Ssl,
    KafkaSecurityProtocol.SaslPlaintext => SecurityProtocol.SaslPlaintext,
    KafkaSecurityProtocol.SaslSsl => SecurityProtocol.SaslSsl,
    _ => SecurityProtocol.Plaintext
  };

  private static SaslMechanism? MapSaslMechanism(KafkaSaslMechanism? m) => m switch
  {
    KafkaSaslMechanism.Plain => SaslMechanism.Plain,
    KafkaSaslMechanism.ScramSha256 => SaslMechanism.ScramSha256,
    KafkaSaslMechanism.ScramSha512 => SaslMechanism.ScramSha512,
    KafkaSaslMechanism.Gssapi => SaslMechanism.Gssapi,
    KafkaSaslMechanism.OAuthBearer => SaslMechanism.OAuthBearer,
    _ => null
  };
}