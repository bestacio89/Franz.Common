using FluentAssertions;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Adapters;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Tests.Adapters;

public class MediatorMessageExtensionsTests
{
  // Updated mock implementations to satisfy your interfaces
  private record TestCommand(string Data) : ICommand;

  private record TestEvent(string Info) : IEvent
  {
    // Satisfying the CS0535 error
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }

    [Fact]
  public void ToMessage_FromCommand_ShouldPopulateRequiredFields()
  {
    // Arrange
    var command = new TestCommand("ProcessData");

    // Act
    var result = command.ToMessage();

    // Assert
    result.Should().NotBeNull();
    result.MessageType.Should().Be(typeof(TestCommand).FullName);
    result.CorrelationId.Should().NotBe(Guid.Empty);

    // Fix for CS0411: Explicitly provide the <string> type argument
    result.GetProperty<string>("CommandType").Should().Be(nameof(TestCommand));

    var deserialized = JsonSerializer.Deserialize<TestCommand>(result.Body);
    deserialized.Should().BeEquivalentTo(command);
  }

  [Fact]
  public void ToMessage_FromEvent_ShouldPopulateRequiredFields()
  {
    // Arrange
    var @event = new TestEvent("DataProcessed");

    // Act
    var result = @event.ToMessage();

    // Assert
    result.Should().NotBeNull();
    result.MessageType.Should().Be(typeof(TestEvent).FullName);

    // Fix for CS0411: Explicitly provide the <string> type argument
    result.GetProperty<string>("EventType").Should().Be(nameof(TestEvent));

    var deserialized = JsonSerializer.Deserialize<TestEvent>(result.Body);
    deserialized.Should().BeEquivalentTo(@event);
  }
}