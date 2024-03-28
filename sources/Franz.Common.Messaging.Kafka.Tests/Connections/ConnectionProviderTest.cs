using NUnit.Framework;
using Moq;
using Confluent.Kafka;
using System;
using Franz.Common.Testing;
using Franz.Common.Messaging.Kafka.Connections;

namespace Franz.Common.Messaging.Kafka.Tests.Connections
{
  [TestFixture]
  public class ConnectionProviderTests : UnitTest
  {
    [Test]
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

    [Test]
    public void Current_Returns_Existing_Connection_When_Connection_Is_Not_Null()
    {
      // Arrange
      var connectionFactoryProviderMock = new Mock<IConnectionFactoryProvider>();
      var connectionProvider = new ConnectionProvider(connectionFactoryProviderMock.Object);
      connectionProvider.GetCurrent(); // create the connection

      // Act
      var result = connectionProvider.GetCurrent();

      // Assert
      Assert.NotNull(result);
      connectionFactoryProviderMock.Verify(m => m.Current, Times.Once);
    }

    [Test]
    public void Dispose_Disposes_Connection_When_Connection_Is_Not_Null()
    {
      // Arrange
      var connectionFactoryProviderMock = new Mock<IConnectionFactoryProvider>();
      var connectionProvider = new ConnectionProvider(connectionFactoryProviderMock.Object);
      var connectionMock = new Mock<IProducer<string, object>>();

      // Act
      connectionProvider.Dispose();

      // Assert
      connectionMock.Verify(m => m.Dispose(), Times.Once);
    }

    [Test]
    public void Dispose_Does_Not_Throw_When_Connection_Is_Null()
    {
      // Arrange
      var connectionFactoryProviderMock = new Mock<IConnectionFactoryProvider>();
      var connectionProvider = new ConnectionProvider(connectionFactoryProviderMock.Object);

      // Act and Assert
      Assert.DoesNotThrow(() => connectionProvider.Dispose());
    }
  }
}
