#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Xunit;

namespace Franz.Common.Messaging.Tests.Storage;

public class MessageStorageTests
{
  [Fact]
  public void ToStored_ShouldHandleNullBody_ByThrowingArgumentNullException()
  {
    // Arrange
    var message = new Message(null);

    // Act
    Action act = () => message.ToStored();

    // Assert
    act.Should().Throw<ArgumentNullException>().WithParameterName("Body");
  }

  [Fact]
  public void ToStored_ShouldPreserveGuidV7AndCleanHeaders()
  {
    // Arrange
    var originalId = Guid.CreateVersion7();
    var message = new Message("{\"test\":\"data\"}")
    {
      Id = originalId,
      MessageType = "Franz.TestEvent",
      CorrelationId = Guid.CreateVersion7()
    };

    // FIX: Use collection expressions [] for header initialization
    message.Headers.Add("X-Tenant-Id", ["tenant-1"]);
    message.Headers.Add("X-Empty", ["   "]); // Should be stripped by the mapper logic
    message.Properties["InternalFlag"] = true;

    // Act
    var stored = message.ToStored();

    // Assert
    stored.Id.Should().Be(originalId);
    stored.Body.Should().Be(message.Body);
    stored.Headers.Should().ContainKey("X-Tenant-Id");

    // Senior Note: ToStored mapper should filter out whitespace-only headers
    stored.Headers.Should().NotContainKey("X-Empty");
    stored.Properties["InternalFlag"].Should().Be(true);
    stored.CreatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void ToMessage_ShouldRestoreComplexStringValues()
  {
    // Arrange
    var stored = new StoredMessage
    {
      Id = Guid.CreateVersion7(),
      Body = "payload",
      MessageType = "Franz.Tests.TestEvent, Franz.Tests",
      CorrelationId = Guid.CreateVersion7(),
      Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Roles", ["Admin", "User"] }
            }
    };

    // Act
    var message = stored.ToMessage();

    // Assert
    message.MessageType.Should().Be(stored.MessageType);
    message.CorrelationId.Should().Be(stored.CorrelationId);

    // FIX: headers.TryGetValue now outputs string[] instead of StringValues
    message.Headers.TryGetValue("X-Roles", out var values).Should().BeTrue();
    values!.Length.Should().Be(2);
    values.Should().Contain("Admin").And.Contain("User");
  }

  [Fact]
  public void ToStoredAndBack_ShouldPreserveCompleteMessageEnvelope()
  {
    var messageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var aggregateId = Guid.CreateVersion7();
    var causationId = Guid.CreateVersion7();
    var occurredOn = new DateTimeOffset(2026, 11, 9, 18, 30, 0, TimeSpan.Zero);
    var message = new Message("integration-event-payload")
    {
      Id = messageId,
      Kind = MessageKind.IntegrationEvent,
      CorrelationId = correlationId,
      AggregateId = aggregateId,
      OccurredOn = occurredOn,
      MessageType = "OrderPlaced, Contracts"
    };
    message.Headers[Message.CausationHeader] = [causationId.ToString("D")];
    message.Headers["TenantId"] = [Guid.CreateVersion7().ToString("D")];
    message.Properties["CustomProperty"] = "custom-value";

    var restored = message.ToStored().ToMessage();

    restored.Id.Should().Be(messageId);
    restored.Kind.Should().Be(MessageKind.IntegrationEvent);
    restored.CorrelationId.Should().Be(correlationId);
    restored.AggregateId.Should().Be(aggregateId);
    restored.OccurredOn.Should().Be(occurredOn);
    restored.MessageType.Should().Be("OrderPlaced, Contracts");
    restored.Headers[Message.CausationHeader].Should().ContainSingle(causationId.ToString("D"));
    restored.Headers.Should().ContainKey("TenantId");
    restored.Properties["CustomProperty"].Should().Be("custom-value");
    restored.Properties.Should().NotContainKey("$franz.storage.message-kind");
    restored.Properties.Should().NotContainKey("$franz.storage.aggregate-id");
    restored.Properties.Should().NotContainKey("$franz.storage.occurred-on");
  }

  [Fact]
  public void ToMessage_WhenReadingLegacyStoredMessage_UsesCommandKindAndNoAggregateId()
  {
    var stored = new StoredMessage
    {
      Id = Guid.CreateVersion7(),
      Body = "legacy-payload",
      Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase),
      Properties = new Dictionary<string, object>()
    };

    var restored = stored.ToMessage();

    restored.Kind.Should().Be(MessageKind.Command);
    restored.AggregateId.Should().BeNull();
    restored.CorrelationId.Should().BeNull();
  }

  [Fact]
  public void StoredMessage_Constructor_ShouldDefaultToGuidV7()
  {
    // Act
    var stored = new StoredMessage();

    // Assert
    // Verify Guid Version 7 (the character at index 14 in the string representation)
    stored.Id.ToString()[14].Should().Be('7');
  }
}