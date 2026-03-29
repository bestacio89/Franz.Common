#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka.Connections;

/// <summary>
/// Provides Kafka Producer configuration based on MessagingOptions.
/// Senior Note: Implements strict null-checking to support multi-transport (Kafka/RabbitMQ) environments.
/// </summary>
public sealed class ConnectionFactoryProvider(IOptions<KafkaMessagingOptions> messagingOptions)
    : IConnectionFactoryProvider
{
  private readonly IOptions<KafkaMessagingOptions> _messagingOptions = messagingOptions;

  public ProducerConfig Current => GetCurrent();

  public ProducerConfig GetCurrent()
  {
    var options = _messagingOptions.Value;

    // --- ARCHITECTURAL FAIL-FAST LOGIC ---
    // We prioritize BootStrapServers. If null, we fall back to HostName.
    // If both are null, we throw an ArgumentException. 
    // This prevents the "localhost" ghost from causing silent failures in RabbitMQ-only environments.
    var bootstrapServers = options.BootStrapServers
        ?? options.HostName
        ?? throw new ArgumentException("Kafka BootstrapServers or HostName must be provided in MessagingOptions.");

    return new ProducerConfig
    {
      BootstrapServers = bootstrapServers,

      // Security Mapping: Default to Plaintext if SslEnabled is null or false
      SecurityProtocol = options.SslEnabled == true
            ? SecurityProtocol.Ssl
            : SecurityProtocol.Plaintext,

      // SSL Credentials (only relevant if SecurityProtocol is Ssl)
      SslCaLocation = options.SslCaLocation,
      SslCertificateLocation = options.SslCertificateLocation,
      SslKeyLocation = options.SslKeyLocation,

      // --- .NET 10 / High-Throughput Standards ---
      Acks = Acks.All,             // Ensure data safety
      EnableIdempotence = true,    // Prevent duplicate messages on retry
      LingerMs = 5,                // Optimize batching without significant latency
      MessageTimeoutMs = 30000     // Standard 30s timeout
    };
  }
}