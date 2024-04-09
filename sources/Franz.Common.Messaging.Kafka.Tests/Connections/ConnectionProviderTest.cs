using System;
using Xunit;
using Moq;
using Confluent.Kafka;
using Franz.Common.Testing;
using Franz.Common.Messaging.Kafka.Connections;

namespace Franz.Common.Messaging.Kafka.Tests.Connections
{
  public class ConnectionProviderTests
  {
    [Fact]
    public void Current_Returns_New_Connection_When_Connection_Is_Null()
    {
      // Arrange
      var connectionFactoryProviderMock = new Mock<IConnectionFactoryProvider>();
      var connectionProvider = new ConnectionProvider(connectionFactoryProviderMock.Object);

      // Act
      var result = connectionProvider.Current;

      // Assert
      Assert.NotNull(result);
      connectionFactoryProviderMock.Verify(m => m.Current, Times.Once);
    }

    [Fact]
    public void Current_Returns_Existing_Connection_When_Connection_Is_Not_Null()
    {
      // Arrange
      var connectionFactoryProviderMock = new Mock<IConnectionFactoryProvider>();
      var connection = new Mock<IProducer<string, object>>().Object; // Simulate existing connection
      connectionFactoryProviderMock.Setup(m => m.Current).Returns((ProducerConfig)connection);
      var connectionProvider = new ConnectionProvider(connectionFactoryProviderMock.Object);

      // Act
      var result = connectionProvider.Current;

      // Assert
      Assert.NotNull(result);
      Assert.Same(connection, result); // Verify it's the same instance
      connectionFactoryProviderMock.Verify(m => m.Current, Times.Once);
    }

    [Fact]
    public void Dispose_Disposes_Connection_When_Connection_Is_Not_Null()
    {
      // Arrange
      var connectionFactoryProviderMock = new Mock<IConnectionFactoryProvider>();
      var connectionMock = new Mock<IProducer<string, object>>();
      connectionFactoryProviderMock.Setup(m => m.Current).Returns((ProducerConfig)connectionMock.Object);
      var connectionProvider = new ConnectionProvider(connectionFactoryProviderMock.Object);

      // Act
      connectionProvider.Dispose();

      // Assert
      connectionMock.Verify(m => m.Dispose(), Times.Once);
    }


  }
}
