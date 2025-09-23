using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;

namespace Franz.Common.Messaging.Kafka.Connections;

public class ConnectionFactoryProvider : IConnectionFactoryProvider
{
  private readonly IOptions<MessagingOptions> _messagingOptions;

  public ConnectionFactoryProvider(IOptions<MessagingOptions> messagingOptions)
  {
    _messagingOptions = messagingOptions;
  }

  ProducerConfig IConnectionFactoryProvider.Current => GetCurrent();

  public ProducerConfig GetCurrent()
  {
    var options = _messagingOptions.Value;

    var config = new ProducerConfig
    {
      BootstrapServers = options.HostName ?? "localhost",
      SslCaLocation = options.SslCaLocation,
      SslCertificateLocation = options.SslCertificateLocation,
      SslKeyLocation = options.SslKeyLocation,
      SecurityProtocol = options.SslEnabled == true
          ? SecurityProtocol.Ssl
          : SecurityProtocol.Plaintext
    };

    return config;
  }
}
