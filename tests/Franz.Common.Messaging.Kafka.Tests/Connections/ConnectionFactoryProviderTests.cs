#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
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
  public void Current_Should_Reflect_Options_Settings()
  {
    // Arrange
    var options = new KafkaMessagingOptions
    {
      GroupId = $"test-group-{Guid.NewGuid():N}",
      BootstrapServers = "broker:9092",
      ClientId = "client-1",
      Producer = new KafkaProducerOptions
      {
        Acks = KafkaAcks.Leader,
        EnableIdempotence = false,
        LingerMs = 10
      },
      Security = new KafkaSecurityOptions
      {
        SecurityProtocol = KafkaSecurityProtocol.SaslSsl,
        SaslMechanism = KafkaSaslMechanism.ScramSha256,
        SaslUsername = "user",
        SaslPassword = "pass"
      }
    };

    var provider = CreateProvider(options);

    // Act
    var config = provider.Current;

    // Assert
    config.BootstrapServers.Should().Be("broker:9092");
    config.ClientId.Should().Be("client-1");
    config.Acks.Should().Be(Acks.Leader);
    config.EnableIdempotence.Should().BeFalse();
    config.LingerMs.Should().Be(10);
    config.SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
    config.SaslMechanism.Should().Be(SaslMechanism.ScramSha256);
    config.SaslUsername.Should().Be("user");
    config.SaslPassword.Should().Be("pass");
  }

  [Fact]
  public void Current_Should_Return_New_Instance_Each_Time()
  {
    // Arrange
    var options = new KafkaMessagingOptions { BootstrapServers = "broker:9092", GroupId = $"test-group-{Guid.NewGuid():N}" };
    var provider = CreateProvider(options);

    // Act
    var first = provider.Current;
    var second = provider.Current;

    // Assert
    first.Should().NotBeSameAs(second);
    first.BootstrapServers.Should().Be(second.BootstrapServers);
  }

}