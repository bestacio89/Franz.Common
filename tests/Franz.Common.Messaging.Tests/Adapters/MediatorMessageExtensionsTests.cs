using FluentAssertions;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Adapters;
using Franz.Common.Messaging.Serialization;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Tests.Adapters;

public class MediatorMessageExtensionsTests
{
  private static readonly IMessageSerializer Serializer = new JsonMessageSerializer();
  private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

  private record TestCommand(string Data) : ICommand;
  private record TestEvent(string Info) : IEvent
  {
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
  }

  [Fact]
  public void ToMessage_FromCommand_ShouldPopulateRequiredFields()
  {
    var command = new TestCommand("ProcessData");

    var result = command.ToMessage(Serializer);

    result.Should().NotBeNull();
    result.MessageType.Should().Be(typeof(TestCommand).FullName);
    result.CorrelationId.Should().NotBe(Guid.Empty);
    result.GetProperty<string>("CommandType").Should().Be(nameof(TestCommand));

    var deserialized = JsonSerializer.Deserialize<TestCommand>(result.Body!, Options);
    deserialized.Should().BeEquivalentTo(command);
  }

  [Fact]
  public void ToMessage_FromEvent_ShouldPopulateRequiredFields()
  {
    var @event = new TestEvent("DataProcessed");

    var result = @event.ToMessage(Serializer);

    result.Should().NotBeNull();
    result.MessageType.Should().Be(typeof(TestEvent).FullName);
    result.GetProperty<string>("EventType").Should().Be(nameof(TestEvent));

    var deserialized = JsonSerializer.Deserialize<TestEvent>(result.Body!, Options);
    deserialized.Should().BeEquivalentTo(@event);
  }
}
