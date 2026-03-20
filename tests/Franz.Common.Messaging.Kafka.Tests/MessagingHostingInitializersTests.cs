#nullable enable
using Franz.Common.Hosting;
using Franz.Common.Messaging.Kafka;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Hosting
{
  public class MessagingHostingInitializerTests
  {
    [Fact]
    public void Initialize_CallsUnderlyingMessagingInitializer()
    {
      // Arrange
      var messagingMock = new Mock<IMessagingInitializer>();
      var hostingInitializer = new MessagingHostingInitializer(messagingMock.Object);

      // Act
      hostingInitializer.Initialize();

      // Assert
      messagingMock.Verify(m => m.Initialize(), Times.Once);
    }

    [Fact]
    public void Initialize_WithNullDoesNotThrow()
    {
      // Arrange
      var hostingInitializer = new MessagingHostingInitializer(null);

      // Act / Assert
      var ex = Record.Exception(() => hostingInitializer.Initialize());
      Assert.Null(ex); // Should not throw
    }

    [Fact]
    public void Order_IsCorrect()
    {
      var hostingInitializer = new MessagingHostingInitializer(null);
      Assert.Equal(2, hostingInitializer.Order);
    }
  }
}