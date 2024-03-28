using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using System;

namespace Franz.Common.Messaging.Kafka.Connections;
public class ConnectionFactoryProvider : IConnectionFactoryProvider
{
  private readonly IOptions<MessagingOptions> messagingOptions;


  public ConnectionFactoryProvider(IOptions<MessagingOptions> messagingOptions)
  {
    this.messagingOptions = messagingOptions;
  }

  

   ProducerConfig IConnectionFactoryProvider.Current => GetCurrent();

  public ProducerConfig GetCurrent()
  {
    var config = new ProducerConfig
    {
      BootstrapServers = messagingOptions.Value.HostName ?? "localhost",
      SslCaLocation = messagingOptions.Value.SslCaLocation,
      SslCertificateLocation = messagingOptions.Value.SslCertificateLocation,
      SslKeyLocation = messagingOptions.Value.SslKeyLocation,
      SecurityProtocol = (bool)messagingOptions.Value.SslEnabled ? SecurityProtocol.Ssl : SecurityProtocol.Plaintext
    };
    return config;
  }
}
