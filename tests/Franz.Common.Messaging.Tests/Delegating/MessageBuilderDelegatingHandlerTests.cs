using FluentAssertions;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Delegating;

public class MessageBuilderDelegatingHandlerTests
{
  [Fact]
  public void Process_ShouldOnlyInvokeBuildersThatCanBuild()
  {
    // Arrange
    var message = new Message("{}");

    // Builder 1: Should be invoked
    var builder1 = new Mock<IMessageBuilder>();
    builder1.Setup(b => b.CanBuild(message)).Returns(true);

    // Builder 2: Should NOT be invoked
    var builder2 = new Mock<IMessageBuilder>();
    builder2.Setup(b => b.CanBuild(message)).Returns(false);

    var builders = new List<IMessageBuilder> { builder1.Object, builder2.Object };
    var handler = new MessageBuilderDelegatingHandler(builders);

    // Act
    handler.Process(message);

    // Assert
    builder1.Verify(b => b.Build(message), Times.Once);
    builder2.Verify(b => b.Build(message), Times.Never);
  }

  [Fact]
  public void Process_WithMultipleEligibleBuilders_ShouldInvokeAll()
  {
    // Arrange
    var message = new Message("{}");
    var builder1 = new Mock<IMessageBuilder>();
    var builder2 = new Mock<IMessageBuilder>();

    builder1.Setup(b => b.CanBuild(message)).Returns(true);
    builder2.Setup(b => b.CanBuild(message)).Returns(true);

    var builders = new List<IMessageBuilder> { builder1.Object, builder2.Object };
    var handler = new MessageBuilderDelegatingHandler(builders);

    // Act
    handler.Process(message);

    // Assert
    builder1.Verify(b => b.Build(message), Times.Once);
    builder2.Verify(b => b.Build(message), Times.Once);
  }

  [Fact]
  public void Process_WithNoBuilders_ShouldNotThrowException()
  {
    // Arrange
    var message = new Message("{}");
    var handler = new MessageBuilderDelegatingHandler(Enumerable.Empty<IMessageBuilder>());

    // Act
    Action act = () => handler.Process(message);

    // Assert
    act.Should().NotThrow();
  }
}