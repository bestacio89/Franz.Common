using System;
using Xunit;
using Franz.Common.Messaging.Configuration;
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Testing;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka.Tests.Connections
{
  public class ConnectionFactoryProviderTests
  {
    [Fact]
    public void GetCurrent_Returns_Correct_Configuration_With_Default_Values()
    {
      // Arrange
      var messagingOptions = new MessagingOptions();
      var optionsMock = Options.Create(messagingOptions);
      var connectionFactoryProvider = new ConnectionFactoryProvider(optionsMock);

      // Act
      var config = connectionFactoryProvider.GetCurrent();

      // Assert
      Assert.Equal("localhost", config.BootstrapServers);
      Assert.Null(config.SslCaLocation);
      Assert.Null(config.SslCertificateLocation);
      Assert.Null(config.SslKeyLocation);
      Assert.Equal(SecurityProtocol.Plaintext, config.SecurityProtocol);
    }

    [Fact]
    public void GetCurrent_Returns_Correct_Configuration_With_Ssl_Enabled()
    {
      // Arrange
      var messagingOptions = new MessagingOptions
      {
        HostName = "host",
        SslEnabled = true,
        SslCaLocation = "ca.crt",
        SslCertificateLocation = "cert.crt",
        SslKeyLocation = "key.pem"
      };
      var optionsMock = Options.Create(messagingOptions);
      var connectionFactoryProvider = new ConnectionFactoryProvider(optionsMock);

      // Act
      var config = connectionFactoryProvider.GetCurrent();

      // Assert
      Assert.Equal("host", config.BootstrapServers);
      Assert.Equal("ca.crt", config.SslCaLocation);
      Assert.Equal("cert.crt", config.SslCertificateLocation);
      Assert.Equal("key.pem", config.SslKeyLocation);
      Assert.Equal(SecurityProtocol.Ssl, config.SecurityProtocol);
    }
  }
}
