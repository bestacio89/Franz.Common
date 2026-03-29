#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Connections;

[Collection("KafkaConnections")]
public sealed class ConnectionProviderTests
{
  private readonly Mock<IConnectionFactoryProvider> _factoryProviderMock = new();
  private readonly KafkaContainerFixture _fixture;

  public ConnectionProviderTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;
  }

  private ProducerConfig CreateValidConfig()
  {
    // FIX: Clean the BootstrapServers string. Kafka expects "host:port", 
    // but Testcontainers returns "plaintext://host:port/".
    var cleanedAddress = _fixture.BootstrapServers
        .Replace("plaintext://", "", StringComparison.OrdinalIgnoreCase)
        .TrimEnd('/');

    return new ProducerConfig { BootstrapServers = cleanedAddress };
  }

  [Fact]
  public void Current_ShouldInitializeNewProducer_OnFirstAccess()
  {
    // Arrange
    _factoryProviderMock.Setup(m => m.Current).Returns(CreateValidConfig());
    var sut = new ConnectionProvider(_factoryProviderMock.Object);

    // Act
    var result = sut.Current;

    // Assert
    result.Should().NotBeNull();
    _factoryProviderMock.Verify(m => m.Current, Times.Once);
  }

  [Fact]
  public void Current_ShouldReturnSameInstance_OnSubsequentAccesses()
  {
    // Arrange
    _factoryProviderMock.Setup(m => m.Current).Returns(CreateValidConfig());
    var sut = new ConnectionProvider(_factoryProviderMock.Object);

    // Act
    var first = sut.Current;
    var second = sut.Current;

    // Assert
    second.Should().BeSameAs(first);
  }

  [Fact]
  public async Task DisposeAsync_ShouldBeIdempotent_AndHandleUninitializedState()
  {
    // Arrange
    var sut = new ConnectionProvider(_factoryProviderMock.Object);

    // Act & Assert
    await sut.Awaiting(s => s.DisposeAsync()).Should().NotThrowAsync();
    await sut.Awaiting(s => s.DisposeAsync()).Should().NotThrowAsync();
  }
}