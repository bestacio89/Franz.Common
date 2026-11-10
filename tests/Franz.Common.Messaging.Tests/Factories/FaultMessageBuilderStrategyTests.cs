using System.Text.Json;
using FluentAssertions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Factories;

public class ExecutionFaultMessageBuilderStrategyTests
{
  private readonly ExecutionFaultMessageBuilderStrategy _strategy = new();

  [Theory]
  [InlineData(true)] // Test with Exception
  [InlineData(false)] // Test with IExecutionFault
  public void CanBuild_WhenValueIsExceptionOrFault_ShouldReturnTrue(bool useException)
  {
    // Arrange
    object value = useException
        ? new InvalidOperationException("Error")
        : new Mock<IExecutionFault>().Object;

    // Act
    var result = _strategy.CanBuild(value);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void CanBuild_WhenValueIsInvalid_ShouldReturnFalse()
  {
    // Arrange
    var value = "Not an exception";

    // Act
    var result = _strategy.CanBuild(value);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void Build_WhenValueIsIExecutionFault_ShouldReturnConcreteFaultMessage()
  {
    // Arrange
    var occurredAt = DateTimeOffset.UtcNow;
    var mockFault = new Mock<IExecutionFault>();
    mockFault.Setup(f => f.Code).Returns("ERR_001");
    mockFault.Setup(f => f.Message).Returns("System Error");
    mockFault.Setup(f => f.Source).Returns("Messaging.Tests");
    mockFault.Setup(f => f.StackTrace).Returns("fault-stack");
    mockFault.Setup(f => f.OccurredAt).Returns(occurredAt);

    // Act
    var message = _strategy.Build(mockFault.Object);

    // Assert
    message.Kind.Should().Be(MessageKind.Fault);
    message.MessageType.Should().Be(nameof(ExecutionFault));
    message.Body.Should().NotBeNullOrWhiteSpace();
    message.Body.Should().NotContain("Castle.Proxies");

    var fault = JsonSerializer.Deserialize<ExecutionFault>(message.Body!, FranzJson.Default);
    fault.Should().NotBeNull();
    fault!.Code.Should().Be("ERR_001");
    fault.Message.Should().Be("System Error");
    fault.Source.Should().Be("Messaging.Tests");
    fault.StackTrace.Should().Be("fault-stack");
    fault.OccurredAt.Should().Be(occurredAt);

    // Headers
    message.Headers[MessagingConstants.ClassName].Should().BeEquivalentTo(new StringValues(nameof(ExecutionFault)));
    message.Headers[MessagingConstants.FaultCode].Should().BeEquivalentTo(new StringValues("ERR_001"));
  }

  [Fact]
  public void Build_WhenValueIsException_ShouldConvertAndReturnMessage()
  {
    // Arrange
    var exception = new Exception("Critical Failure");

    // Act
    var message = _strategy.Build(exception);

    // Assert
    message.Kind.Should().Be(MessageKind.Fault);
    message.Body.Should().NotBeNullOrWhiteSpace();

    // Verify internal deserialization of the body matches ExecutionFault structure
    var fault = JsonSerializer.Deserialize<ExecutionFault>(message.Body, FranzJson.Default);
    fault.Should().NotBeNull();
    fault!.Message.Should().Be("Critical Failure");

    // Verify fault code header exists (assuming FromException provides a default code)
    message.Headers.ContainsKey(MessagingConstants.FaultCode).Should().BeTrue();
  }

  [Fact]
  public void Build_WhenValueIsInvalidType_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var invalidValue = new { Data = "Invalid" };

    // Act
    Action act = () => _strategy.Build(invalidValue);

    // Assert
    act.Should().Throw<InvalidOperationException>();
  }
}
