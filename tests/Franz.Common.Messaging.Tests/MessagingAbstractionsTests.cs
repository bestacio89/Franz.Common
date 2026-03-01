using FluentAssertions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Hosting;
using Xunit;

namespace Franz.Common.Messaging.Tests;

public class MessagingAbstractionsTests
{
  #region MessagePropertiesExtensions Tests

  [Fact]
  public void TryGetProperty_ShouldReturnTrue_WhenTypeMatches()
  {
    // Arrange
    var message = new Message();
    message.SetProperty("RetryCount", 5);

    // Act
    bool success = message.TryGetProperty<int>("RetryCount", out var value);

    // Assert
    success.Should().BeTrue();
    value.Should().Be(5);
  }

  [Fact]
  public void TryGetProperty_ShouldConvertToString_WhenRequestedAsText()
  {
    // Arrange
    var message = new Message();
    var correlationId = Guid.NewGuid();
    message.SetProperty("TraceId", correlationId);

    // Act - Requesting a Guid as a string
    bool success = message.TryGetProperty<string>("TraceId", out var value);

    // Assert
    success.Should().BeTrue();
    value.Should().Be(correlationId.ToString());
  }

  [Fact]
  public void TryGetProperty_ShouldReturnFalse_WhenKeyDoesNotExist()
  {
    // Arrange
    var message = new Message();

    // Act
    bool success = message.TryGetProperty<string>("NonExistent", out var value);

    // Assert
    success.Should().BeFalse();
    value.Should().BeNull();
  }

  #endregion

  #region MessagingConstants Tests

  [Fact]
  public void MessagingConstants_ShouldHaveExpectedValues()
  {
    // Assert - Ensuring these don't change accidentally during refactors
    MessagingConstants.MessageId.Should().Be("MessageId");
    MessagingConstants.ClassName.Should().Be("ClassName");
    MessagingConstants.FaultCode.Should().Be("FaultCode");
  }

  #endregion

  #region MessagingEvent Tests

  [Fact]
  public void MessageEventArgs_ShouldHoldReferenceToMessage()
  {
    // Arrange
    var message = new Message("Hello World");

    // Act
    var args = new MessageEventArgs(message);

    // Assert
    args.Message.Should().BeSameAs(message);
    args.Message.Body.Should().Be("Hello World");
  }

  #endregion
}