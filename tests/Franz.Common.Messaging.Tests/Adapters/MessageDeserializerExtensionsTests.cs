using FluentAssertions;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Messaging.Adapters;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Manifest;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Tests.Adapters;

public class MessageDeserializerExtensionsTests
{
  static MessageDeserializerExtensionsTests()
  {
    var assembly = typeof(MessageDeserializerExtensionsTests).Assembly;
    var existing = MessagingManifestRegistry.Get(assembly);
    MessagingManifestRegistry.Register(assembly, new MessagingManifest(
        existing.Destinations,
        existing.Subscriptions,
        existing.KnownTypes.Concat(new[] { typeof(TestCommand), typeof(TestEvent) })));
  }
  // Define concrete types for testing reflection logic
  public record TestCommand(string Data) : ICommand
  {
    public Guid CorrelationId { get; set; }
  }

  public record TestEvent(string Info) : IEvent
  {
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public Guid CorrelationId { get; set; }
  }

  [Fact]
  public void ToCommand_WithValidMessage_ShouldDeserializeAndSetCorrelation()
  {
    // Arrange
    var expectedId = Guid.CreateVersion7();
    var command = new TestCommand("Hello World");
    var json = JsonSerializer.Serialize(command, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    var message = new Message(json)
    {
      MessageType = typeof(TestCommand).FullName, // Use FullName or AssemblyQualifiedName
      CorrelationId = expectedId
    };

    // Act
    var result = message.ToCommand() as TestCommand;

    // Assert
    result.Should().NotBeNull();
    result!.Data.Should().Be("Hello World");
    result.CorrelationId.Should().Be(expectedId);
    MediatorContext.Current.CorrelationId.Should().Be(expectedId); // Verify ambient context
  }

  [Fact]
  public void ToEvent_WithValidMessage_ShouldDeserializeAndSetCorrelation()
  {
    // Arrange
    var expectedId = Guid.CreateVersion7();
    var @event = new TestEvent("EventData");
    var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    var message = new Message(json)
    {
      MessageType = typeof(TestEvent).FullName,
      CorrelationId = expectedId
    };

    // Act
    var result = message.ToEvent() as TestEvent;

    // Assert
    result.Should().NotBeNull();
    result!.Info.Should().Be("EventData");
    result.CorrelationId.Should().Be(expectedId);
    MediatorContext.Current.CorrelationId.Should().Be(expectedId);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData(" ")]
  public void ToCommand_WithInvalidMessageType_ShouldReturnNull(string? invalidType)
  {
    // Arrange
    var message = new Message("{}") { MessageType = invalidType };

    // Act
    var result = message.ToCommand();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void ToCommand_WhenTypeIsNotInManifest_ShouldReturnNull()
  {
    var message = new Message("{}") { MessageType = "Unregistered.Namespace.Command" };
    message.ToCommand().Should().BeNull();
  }

  [Fact]
  public void ToCommand_WhenTypeMismatch_ShouldReturnNull()
  {
    // Arrange - Sending an Event type name to the ToCommand method
    var message = new Message("{}")
    {
      MessageType = typeof(TestEvent).FullName
    };

    // Act
    var result = message.ToCommand();

    // Assert
    result.Should().BeNull();
  }
}