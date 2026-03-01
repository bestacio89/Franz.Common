using FluentAssertions;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Messages;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Contexting;

public class MessageContextTests
{
  [Fact]
  public void MessageContext_Constructor_ShouldSetMessageProperty()
  {
    // Arrange
    var body = "{\"test\":\"data\"}";
    var message = new Message(body);

    // Act
    var context = new MessageContext(message);

    // Assert
    context.Message.Should().NotBeNull();
    context.Message.Should().BeSameAs(message);
    context.Message.Body.Should().Be(body);
  }

  [Fact]
  public void MessageContext_ShouldImplementIMessageContext()
  {
    // Arrange
    var message = new Message("{}");
    var context = new MessageContext(message);

    // Assert
    context.Should().BeAssignableTo<IMessageContext>();
  }

  [Fact]
  public void IMessageContextAccessor_ShouldBeMockable()
  {
    // Arrange
    var message = new Message("{}");
    var context = new MessageContext(message);

    var mockAccessor = new Mock<IMessageContextAccessor>();
    mockAccessor.Setup(x => x.Current).Returns(context);

    // Act
    var currentContext = mockAccessor.Object.Current;

    // Assert
    currentContext.Should().NotBeNull();
    currentContext!.Message.Should().BeSameAs(message);
  }
}