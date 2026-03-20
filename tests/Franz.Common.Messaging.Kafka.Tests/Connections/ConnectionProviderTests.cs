using Confluent.Kafka;
using FluentAssertions;
using Moq;
using Franz.Common.Messaging.Kafka.Connections;

namespace Franz.Common.Messaging.Kafka.Tests.Connections;

public class ConnectionProviderTests
{
  private static ProducerConfig CreateValidConfig() =>
      new()
      {
        BootstrapServers = "localhost:9092"
      };

  [Fact]
  public void GetCurrent_Should_Create_Producer_On_First_Call()
  {
    // Arrange
    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(f => f.Current)
               .Returns(CreateValidConfig());

    var provider = new ConnectionProvider(factoryMock.Object);

    // Act
    var producer = provider.GetCurrent();

    // Assert
    producer.Should().NotBeNull();
    factoryMock.Verify(f => f.Current, Times.Once);
  }

  [Fact]
  public void GetCurrent_Should_Return_Same_Instance_On_Subsequent_Calls()
  {
    // Arrange
    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(f => f.Current)
               .Returns(CreateValidConfig());

    var provider = new ConnectionProvider(factoryMock.Object);

    // Act
    var first = provider.GetCurrent();
    var second = provider.GetCurrent();

    // Assert
    first.Should().BeSameAs(second);
    factoryMock.Verify(f => f.Current, Times.Once);
  }

  [Fact]
  public void Current_Property_Should_Delegate_To_GetCurrent()
  {
    // Arrange
    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(f => f.Current)
               .Returns(CreateValidConfig());

    var provider = new ConnectionProvider(factoryMock.Object);

    // Act
    var fromProperty = provider.Current;
    var fromMethod = provider.GetCurrent();

    // Assert
    fromProperty.Should().BeSameAs(fromMethod);
  }

  [Fact]
  public void Dispose_Should_Dispose_Producer()
  {
    // Arrange
    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(f => f.Current)
               .Returns(CreateValidConfig());

    var provider = new ConnectionProvider(factoryMock.Object);

    var producer = provider.GetCurrent();

    // Act
    provider.Dispose();

    // Assert
    Action act = () => producer.Produce("topic", new Message<string, object>
    {
      Key = "key",
      Value = new object()
    });

    act.Should().Throw<ObjectDisposedException>();
  }
}