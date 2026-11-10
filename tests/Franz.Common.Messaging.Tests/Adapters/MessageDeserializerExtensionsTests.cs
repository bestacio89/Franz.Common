using FluentAssertions;
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Logging;
using Franz.Common.Messaging.Adapters;
using Franz.Common.Messaging.Manifest;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
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
        existing.Subscriptions.Concat(new[] { typeof(TestEvent), typeof(NoCorrelationTestEvent) }),
        existing.KnownTypes.Concat(new[] { typeof(TestCommand), typeof(TestEvent), typeof(UnsubscribedTestEvent), typeof(NoCorrelationTestEvent) })));
  }

  public record TestCommand(string Data) : ICommand, ICorrelatedMessage
  {
    public Guid? CorrelationId { get; set; }
  }

  public record TestEvent(string Info) : IEvent, ICorrelatedMessage
  {
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public Guid? CorrelationId { get; set; }
  }

  public record UnsubscribedTestEvent(string Info) : IEvent
  {
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
  }

  public record LateRegisteredTestEvent(string Info) : IEvent
  {
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
  }

  public record NoCorrelationTestEvent(string Info) : IEvent
  {
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
  }

  private static readonly IMessageSerializer Serializer = new JsonMessageSerializer();

  [Fact]
  public void ToEvent_WhenTypeIsKnownButNotSubscribed_ShouldReturnNull()
  {
    var message = new Message("{}") { MessageType = typeof(UnsubscribedTestEvent).FullName };
    message.ToEvent(Serializer).Should().BeNull();
  }

  [Fact]
  public void ToEvent_WhenManifestIsRegisteredAfterAnInitialMiss_ShouldResolveOnTheNextLookup()
  {
    var message = new Message(JsonSerializer.Serialize(new LateRegisteredTestEvent("late"), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
    {
      MessageType = typeof(LateRegisteredTestEvent).FullName
    };

    message.ToEvent(Serializer).Should().BeNull();

    var assembly = typeof(MessageDeserializerExtensionsTests).Assembly;
    var existing = MessagingManifestRegistry.Get(assembly);
    MessagingManifestRegistry.Register(assembly, new MessagingManifest(
        existing.Destinations,
        existing.Subscriptions.Concat(new[] { typeof(LateRegisteredTestEvent) }),
        existing.KnownTypes.Concat(new[] { typeof(LateRegisteredTestEvent) })));

    var result = message.ToEvent(Serializer);

    result.Should().BeOfType<LateRegisteredTestEvent>()
        .Which.Info.Should().Be("late");
  }

  [Fact]
  public void ToEvent_WithoutCorrelationContract_ShouldDeserializeWithoutCorrelationHydration()
  {
    var message = new Message(JsonSerializer.Serialize(new NoCorrelationTestEvent("no-correlation"), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
    {
      MessageType = typeof(NoCorrelationTestEvent).FullName
    };

    var result = message.ToEvent(Serializer);

    result.Should().BeOfType<NoCorrelationTestEvent>()
        .Which.Info.Should().Be("no-correlation");
  }

  [Fact]
  public void ToCommand_WithValidMessage_ShouldDeserializeAndSetCorrelation()
  {
    var expectedId = Guid.CreateVersion7();
    var command = new TestCommand("Hello World");
    var json = JsonSerializer.Serialize(command, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    var message = new Message(json)
    {
      MessageType = typeof(TestCommand).FullName,
      CorrelationId = expectedId
    };

    var result = message.ToCommand(Serializer) as TestCommand;

    result.Should().NotBeNull();
    result!.Data.Should().Be("Hello World");
    result.CorrelationId.Should().Be(expectedId);
    MediatorContext.Current.CorrelationId.Should().Be(expectedId);
  }

  [Fact]
  public void ToEvent_WithValidMessage_ShouldDeserializeAndSetCorrelation()
  {
    var expectedId = Guid.CreateVersion7();
    var @event = new TestEvent("EventData");
    var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    var message = new Message(json)
    {
      MessageType = typeof(TestEvent).FullName,
      CorrelationId = expectedId
    };

    var result = message.ToEvent(Serializer) as TestEvent;

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
    var message = new Message("{}") { MessageType = invalidType };
    message.ToCommand(Serializer).Should().BeNull();
  }

  [Fact]
  public void ToCommand_WhenTypeIsNotInManifest_ShouldReturnNull()
  {
    var message = new Message("{}") { MessageType = "Unregistered.Namespace.Command" };
    message.ToCommand(Serializer).Should().BeNull();
  }

  [Fact]
  public void ToCommand_WhenTypeMismatch_ShouldReturnNull()
  {
    var message = new Message("{}")
    {
      MessageType = typeof(TestEvent).FullName
    };

    message.ToCommand(Serializer).Should().BeNull();
  }
}
