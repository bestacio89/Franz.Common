using FluentAssertions;
using Franz.Common.Messaging.Messages;
using Xunit;

namespace Franz.Common.Messaging.Tests.Messages;

public class ExecutionFaultTests
{
  [Fact]
  public void Constructor_ShouldInitializePropertiesCorrectly()
  {
    // Arrange
    var code = "ERR_404";
    var message = "Resource not found";
    var source = "OrderService";
    var stackTrace = "at SomeInternalMethod()";

    // Act
    var fault = new ExecutionFault(code, message, source, stackTrace);

    // Assert
    fault.Code.Should().Be(code);
    fault.Message.Should().Be(message);
    fault.Source.Should().Be(source);
    fault.StackTrace.Should().Be(stackTrace);
    fault.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void FromException_ShouldMapExceptionDetailsToFault()
  {
    // Arrange
    var innerEx = new InvalidOperationException("Inner failure");
    try
    {
      throw new ArgumentException("Invalid input", "parameter", innerEx)
      {
        Source = "ValidationComponent"
      };
    }
    catch (Exception ex)
    {
      // Act
      var fault = ExecutionFault.FromException(ex);

      // Assert
      fault.Code.Should().Be(nameof(ArgumentException));
      fault.Message.Should().Be("Invalid input (Parameter 'parameter')");
      fault.Source.Should().Be("ValidationComponent");
      fault.StackTrace.Should().NotBeNullOrWhiteSpace();
      fault.StackTrace.Should().Contain("FromException_ShouldMapExceptionDetailsToFault");
    }
  }

  [Fact]
  public void FromException_WithNullSourceAndStack_ShouldStillSucceed()
  {
    // Arrange
    var ex = new Exception("Simple error");
    // Manually ensure Source is null (some environments auto-populate)
    ex.Source = null;

    // Act
    var fault = ExecutionFault.FromException(ex);

    // Assert
    fault.Code.Should().Be(nameof(Exception));
    fault.Source.Should().BeNull();
    fault.StackTrace.Should().BeNull();
  }

  [Fact]
  public void OccurredAt_ShouldBeSetToUtcNow()
  {
    // Act
    var fault = new ExecutionFault("CODE", "MSG");

    // Assert
    // Verifying it is a recent UTC time
    fault.OccurredAt.Offset.Should().Be(TimeSpan.Zero);
    fault.OccurredAt.Should().BeBefore(DateTimeOffset.UtcNow.AddMilliseconds(100));
  }
}