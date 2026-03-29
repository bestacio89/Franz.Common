#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Tests.Delegating;

/// <summary>
/// Verifies the asynchronous orchestration of message builders within the delegating handler.
/// Senior Architect Note: Using Task-based assertions to prevent deadlocks in high-throughput messaging pipelines.
/// </summary>
public class MessageBuilderDelegatingHandlerTests
{
  [Fact]
  public async Task ProcessAsync_ShouldOnlyInvokeBuildersThatCanBuild()
  {
    // Arrange
    var message = new Message("{}");

    // Builder 1: Should be invoked
    var builder1 = new Mock<IMessageBuilder>();
    builder1.Setup(b => b.CanBuild(message)).Returns(true);
    builder1.Setup(b => b.BuildAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

    // Builder 2: Should NOT be invoked
    var builder2 = new Mock<IMessageBuilder>();
    builder2.Setup(b => b.CanBuild(message)).Returns(false);
    builder2.Setup(b => b.BuildAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    var builders = new List<IMessageBuilder> { builder1.Object, builder2.Object };
    var handler = new MessageBuilderDelegatingHandler(builders);

    // Act
    await handler.ProcessAsync(message);

    // Assert
    builder1.Verify(b => b.BuildAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    builder2.Verify(b => b.BuildAsync(message, It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task ProcessAsync_WithMultipleEligibleBuilders_ShouldInvokeAll()
  {
    // Arrange
    var message = new Message("{}");
    var builder1 = new Mock<IMessageBuilder>();
    var builder2 = new Mock<IMessageBuilder>();

    builder1.Setup(b => b.CanBuild(message)).Returns(true);
    builder1.Setup(b => b.BuildAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    builder2.Setup(b => b.CanBuild(message)).Returns(true);
    builder2.Setup(b => b.BuildAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    var builders = new List<IMessageBuilder> { builder1.Object, builder2.Object };
    var handler = new MessageBuilderDelegatingHandler(builders);

    // Act
    await handler.ProcessAsync(message);

    // Assert
    builder1.Verify(b => b.BuildAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    builder2.Verify(b => b.BuildAsync(message, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ProcessAsync_WithNoBuilders_ShouldNotThrowException()
  {
    // Arrange
    var message = new Message("{}");
    var handler = new MessageBuilderDelegatingHandler(Enumerable.Empty<IMessageBuilder>());

    // Act
    Func<Task> act = async () => await handler.ProcessAsync(message);

    // Assert
    await act.Should().NotThrowAsync();
  }
}