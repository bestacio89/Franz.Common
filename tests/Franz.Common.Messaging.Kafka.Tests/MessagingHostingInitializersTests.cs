#nullable enable
using FluentAssertions;
using Franz.Common.Hosting;
using Franz.Common.Messaging.Kafka;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Hosting;

[Collection("Kafka")]
public class MessagingHostingInitializerTests
{
  [Fact]
  public async Task InitializeAsync_ShouldCallUnderlyingInitializer()
  {
    var messagingMock = new Mock<IMessagingInitializer>();

    messagingMock
        .Setup(m => m.InitializeAsync(It.IsAny<CancellationToken>()))
        .Returns(ValueTask.CompletedTask);

    var hostingInitializer = new MessagingHostingInitializer(messagingMock.Object);

    await hostingInitializer.InitializeAsync();

    messagingMock.Verify(
        m => m.InitializeAsync(It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task InitializeAsync_ShouldBeSafe_WhenInitializerIsNull()
  {
    var hostingInitializer = new MessagingHostingInitializer(null);

    var ex = await Record.ExceptionAsync(() =>
        hostingInitializer.InitializeAsync());

    ex.Should().BeNull();
  }

  [Fact]
  public async Task InitializeAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
  {
    var messagingMock = new Mock<IMessagingInitializer>();

    messagingMock
        .Setup(m => m.InitializeAsync(It.IsAny<CancellationToken>()))
        .Returns(ValueTask.CompletedTask);

    var hostingInitializer = new MessagingHostingInitializer(messagingMock.Object);

    await hostingInitializer.InitializeAsync();
    await hostingInitializer.InitializeAsync();

    messagingMock.Verify(
        m => m.InitializeAsync(It.IsAny<CancellationToken>()),
        Times.Exactly(2));
  }

  [Fact]
  public void Order_ShouldBeStableAndPositive()
  {
    var hostingInitializer = new MessagingHostingInitializer(null);

    hostingInitializer.Order.Should().BeGreaterThan(0);
  }
}