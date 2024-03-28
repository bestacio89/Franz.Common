using NUnit.Framework;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Testing;

namespace Franz.Common.Messaging.Kafka.Tests.Connections
{
  [TestFixture]
  public class ConnectionFactoryProviderTests : UnitTest
  {
    [Test]
    public void GetCurrent_Returns_Correct_Configuration_With_Default_Values()
    {
      // Arrange
      var messagingOptions = new MessagingOptions();
      var optionsMock = Options.Create(messagingOptions);
      var connectionFactoryProvider = new ConnectionFactoryProvider(optionsMock);

      // Act
      var config = connectionFactoryProvider.GetCurrent();

      // Assert
      Assert.AreEqual("localhost", config.BootstrapServers);
      Assert.IsNull(config.SslCaLocation);
      Assert.IsNull(config.SslCertificateLocation);
      Assert.IsNull(config.SslKeyLocation);
      Assert.AreEqual(SecurityProtocol.Plaintext, config.SecurityProtocol);
    }

    [Test]
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
      Assert.AreEqual("host", config.BootstrapServers);
      Assert.AreEqual("ca.crt", config.SslCaLocation);
      Assert.AreEqual("cert.crt", config.SslCertificateLocation);
      Assert.AreEqual("key.pem", config.SslKeyLocation);
      Assert.AreEqual(SecurityProtocol.Ssl, config.SecurityProtocol);
    }
  }
}
