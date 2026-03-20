using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Connections;

namespace Franz.Common.Messaging.Kafka.Tests.Connections;

public class ConnectionFactoryProviderTests
{
  private static ConnectionFactoryProvider CreateProvider(MessagingOptions options)
  {
    var optionsWrapper = Options.Create(options);
    return new ConnectionFactoryProvider(optionsWrapper);
  }

  [Fact]
  public void GetCurrent_Should_Default_To_Localhost_When_HostName_Is_Null()
  {
    // Arrange
    var options = new MessagingOptions
    {
      HostName = null,
      SslEnabled = false
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.GetCurrent();

    // Assert
    config.BootstrapServers.Should().Be("localhost");
    config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
  }

  [Fact]
  public void GetCurrent_Should_Use_Configured_HostName()
  {
    // Arrange
    var options = new MessagingOptions
    {
      HostName = "kafka-broker:9092",
      SslEnabled = false
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.GetCurrent();

    // Assert
    config.BootstrapServers.Should().Be("kafka-broker:9092");
    config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
  }

  [Fact]
  public void GetCurrent_Should_Enable_Ssl_When_Configured()
  {
    // Arrange
    var options = new MessagingOptions
    {
      HostName = "secure-broker:9093",
      SslEnabled = true,
      SslCaLocation = "/ca.pem",
      SslCertificateLocation = "/cert.pem",
      SslKeyLocation = "/key.pem"
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.GetCurrent();

    // Assert
    config.SecurityProtocol.Should().Be(SecurityProtocol.Ssl);
    config.SslCaLocation.Should().Be("/ca.pem");
    config.SslCertificateLocation.Should().Be("/cert.pem");
    config.SslKeyLocation.Should().Be("/key.pem");
  }

  [Fact]
  public void Current_Property_Should_Return_Same_Result_As_GetCurrent()
  {
    // Arrange
    var options = new MessagingOptions
    {
      HostName = "broker",
      SslEnabled = true
    };

    var provider = CreateProvider(options);

    // Act
    var fromProperty = provider.Current;
    var fromMethod = provider.GetCurrent();

    // Assert
    fromProperty.BootstrapServers.Should().Be(fromMethod.BootstrapServers);
    fromProperty.SecurityProtocol.Should().Be(fromMethod.SecurityProtocol);
  }

  [Fact]
  public void GetCurrent_Should_Handle_Null_SslEnabled_As_Plaintext()
  {
    var options = new MessagingOptions
    {
      HostName = "broker",
      SslEnabled = null
    };

    var provider = CreateProvider(options);

    var config = provider.GetCurrent();

    config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
  }
}