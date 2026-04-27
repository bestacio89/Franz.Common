using System.Text.Json;
using FluentAssertions;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Franz.Common.Messaging.Tests.Factories;

public class CommandMessageBuilderStrategyTests
{
  // Mock command for testing
  private record TestCommand(string Name, int Value) : ICommand;
  private record NotACommand(string Data);

  private readonly CommandMessageBuilderStrategy _strategy = new();

  [Fact]
  public void CanBuild_WhenValueIsICommand_ShouldReturnTrue()
  {
    // Arrange
    var command = new TestCommand("Test", 123);

    // Act
    var result = _strategy.CanBuild(command);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void CanBuild_WhenValueIsNotICommand_ShouldReturnFalse()
  {
    // Arrange
    var notACommand = new NotACommand("Ignore me");

    // Act
    var result = _strategy.CanBuild(notACommand);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void Build_ShouldReturnMessageWithCorrectPropertiesAndHeaders()
  {
    // Arrange
    var command = new TestCommand("ProcessOrder", 42);
    var expectedBody = JsonSerializer.Serialize(command, FranzJson.Default);

    // Act
    var message = _strategy.Build(command);

    // Assert
    message.Should().NotBeNull();
    message.Kind.Should().Be(MessageKind.Command);
    message.Body.Should().Be(expectedBody);

    // Verify Headers
    message.Headers.ContainsKey(MessagingConstants.ClassName).Should().BeTrue();

    // HeaderNamer logic check
    var expectedClassName = HeaderNamer.GetEventClassName(typeof(TestCommand));
    message.Headers[MessagingConstants.ClassName].Should().BeEquivalentTo(new StringValues(expectedClassName));
  }

  [Fact]
  public void Build_ShouldResultInValidJsonBody()
  {
    // Arrange
    var command = new TestCommand("JsonTest", 99);

    // Act
    var message = _strategy.Build(command);

    message.Body.Should().NotBeNull();

    var deserialized =
        JsonSerializer.Deserialize<TestCommand>(message.Body!, FranzJson.Default);

    // Assert
    deserialized.Should().NotBeNull();
    deserialized.Should().BeEquivalentTo(command);
  }
}