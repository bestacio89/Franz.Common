using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;

namespace Franz.Common.Messaging.Kafka.Connections;

public class ConnectionFactoryProvider(IOptions<MessagingOptions> messagingOptions)
  : IConnectionFactoryProvider
{
  private readonly IOptions<MessagingOptions> _messagingOptions = messagingOptions;

  public ProducerConfig Current => GetCurrent();

  public ProducerConfig GetCurrent()
  {
    var options = _messagingOptions.Value;

    return new ProducerConfig
    {
      BootstrapServers = options.HostName ?? "localhost",
      SslCaLocation = options.SslCaLocation,
      SslCertificateLocation = options.SslCertificateLocation,
      SslKeyLocation = options.SslKeyLocation,
      SecurityProtocol = options.SslEnabled == true
          ? SecurityProtocol.Ssl
          : SecurityProtocol.Plaintext
    };
  }
}
