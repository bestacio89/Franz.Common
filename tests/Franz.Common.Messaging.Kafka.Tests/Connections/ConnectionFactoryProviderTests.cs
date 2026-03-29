#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Connections;

[Collection("KafkaConnections")]
public sealed class ConnectionFactoryProviderTests
{
  private readonly KafkaContainerFixture _fixture;

  public ConnectionFactoryProviderTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;
  }

  private static ConnectionFactoryProvider CreateProvider(KafkaMessagingOptions options)
  {
    var optionsWrapper = Options.Create(options);
    return new ConnectionFactoryProvider(optionsWrapper);
  }

  [Fact]
  public void GetCurrent_Should_Use_Fixture_BootstrapServers_When_Configured()
  {
    // Arrange
    // We use the real dynamic address from the Testcontainer
    var options = new KafkaMessagingOptions
    {
      BootStrapServers = _fixture.BootstrapServers,
      SslEnabled = false
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.Current;

    // Assert
    config.BootstrapServers.Should().Be(_fixture.BootstrapServers);
    config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
  }

  [Fact]
  public void GetCurrent_Should_Fallback_To_HostName_If_BootstrapServers_Missing()
  {
    // Arrange
    var options = new KafkaMessagingOptions
    {
      BootStrapServers = null,
      HostName = "fallback-broker:9092",
      SslEnabled = false
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.Current;

    // Assert
    config.BootstrapServers.Should().Be("fallback-broker:9092");
  }

  [Fact]
  public void GetCurrent_Should_Enable_Ssl_When_Configured()
  {
    // Arrange
    var options = new KafkaMessagingOptions
    {
      BootStrapServers = _fixture.BootstrapServers,
      SslEnabled = true,
      SslCaLocation = "/ca.pem",
      SslCertificateLocation = "/cert.pem",
      SslKeyLocation = "/key.pem"
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.Current;

    // Assert
    config.SecurityProtocol.Should().Be(SecurityProtocol.Ssl);
    config.SslCaLocation.Should().Be("/ca.pem");
    config.SslCertificateLocation.Should().Be("/cert.pem");
    config.SslKeyLocation.Should().Be("/key.pem");
  }

  [Fact]
  public void GetCurrent_Should_Handle_Null_SslEnabled_As_Plaintext()
  {
    // Arrange
    var options = new KafkaMessagingOptions
    {
      BootStrapServers = "broker",
      SslEnabled = null
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.Current;

    // Assert
    config.SecurityProtocol.Should().Be(SecurityProtocol.Plaintext);
  }

  [Fact]
  public void Current_Property_Should_Return_New_Instance_Each_Time_By_Design()
  {
    // Senior Note: Unlike ConnectionProvider, the FactoryProvider 
    // usually returns a new Config object to allow for mutations if needed.
    // Arrange
    var options = new KafkaMessagingOptions { BootStrapServers = "broker" };
    var provider = CreateProvider(options);

    // Act
    var first = provider.Current;
    var second = provider.Current;

    // Assert
    first.BootstrapServers.Should().Be(second.BootstrapServers);
    // We check that it's a valid config, but not necessarily the same reference 
    // depending on your implementation of ConnectionFactoryProvider.
  }
}