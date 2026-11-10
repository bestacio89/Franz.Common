#nullable enable
using System.Text.Json;
using FluentAssertions;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Xunit;

namespace Franz.Common.Messaging.Tests.Messages;

public class MessageTests
{
  [Fact]
  public void Constructor_ShouldInitializeWithGuidV7()
  {
    // Act
    var message = new Message();

    // Assert
    message.Id.Should().NotBeEmpty();
    // Guid v7 check: version digit '7' is at index 14
    message.Id.ToString()[14].Should().Be('7');
  }



  [Fact]
  public void CorrelationId_WhenHydratedFromPropertiesAsString_ShouldParseAndReturn()
  {
    // Arrange
    var existingCid = Guid.CreateVersion7();
    var message = new Message();

    // Trigger synchronization logic
    message.CorrelationId = existingCid;

    // Act
    var result = message.CorrelationId;

    // Assert
    result.Should().Be(existingCid);
  }

 

  [Fact]
  public void Headers_WhenReplacedWithoutCorrelationHeader_ShouldPreserveCorrelationProjection()
  {
    var correlationId = Guid.CreateVersion7();
    var message = new Message { CorrelationId = correlationId };

    message.Headers = new Dictionary<string, string[]>
    {
      ["Custom-Header"] = ["custom-value"]
    };

    message.CorrelationId.Should().Be(correlationId);
    message.Headers.Should().ContainKey(Message.CorrelationHeader)
        .WhoseValue.Should().ContainSingle(correlationId.ToString("D"));
  }

  [Fact]
  public void Headers_WhenReplacedWithCorrelationHeader_ShouldHydrateCorrelationProperty()
  {
    var correlationId = Guid.CreateVersion7();
    var message = new Message
    {
      Headers = new Dictionary<string, string[]>
      {
        [Message.CorrelationHeader] = [correlationId.ToString("D")]
      }
    };

    message.CorrelationId.Should().Be(correlationId);
  }

  [Fact]
  public void Headers_WhenReplacedWithInvalidCorrelationHeader_ShouldRestoreCurrentProjection()
  {
    var correlationId = Guid.CreateVersion7();
    var message = new Message { CorrelationId = correlationId };

    message.Headers = new Dictionary<string, string[]>
    {
      [Message.CorrelationHeader] = ["not-a-guid"]
    };

    message.CorrelationId.Should().Be(correlationId);
    message.Headers[Message.CorrelationHeader].Should().ContainSingle(correlationId.ToString("D"));
  }

  [Fact]
  public void GetProperty_ShouldDeserializeJsonElementValues()
  {
    var message = new Message();
    using var document = JsonDocument.Parse("{\"count\":42}");
    message.Properties["Payload"] = document.RootElement.Clone();

    message.GetProperty<TestPayload>("Payload")
        .Should().BeEquivalentTo(new TestPayload { Count = 42 });
  }

  [Fact]
  public void GetProperty_ShouldReturnDefault_WhenTypeMismatch()
  {
    // Arrange
    var message = new Message();
    message.Properties["TestKey"] = "not-an-int";

    // Act
    var result = message.GetProperty<int>("TestKey");

    // Assert
    result.Should().Be(0);
  }

  private sealed class TestPayload
  {
    public int Count { get; set; }
  }

  [Fact]
  public void SetProperty_ShouldAllowChainableOrDirectAccess()
  {
    // Arrange
    var message = new Message();
    var msgType = "UserRegisteredEvent";

    // Act
    message.MessageType = msgType;

    // Assert
    message.Properties[nameof(Message.MessageType)].Should().Be(msgType);
    message.GetProperty<string>(nameof(Message.MessageType)).Should().Be(msgType);
  }

  [Fact]
  public void Constructor_WithDictionary_ShouldMapToHeadersCorrectly()
  {
    // Arrange
    // SENIOR FIX: Map the dictionary values to string[] to satisfy IDictionary invariance
    var sourceDict = new Dictionary<string, IReadOnlyCollection<string>>
        {
            { "X-Tenant-Id", ["tenant-1"] },
            { "X-Tags", ["tag1", "tag2"] }
        };

    var headers = sourceDict.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToArray(),
        StringComparer.OrdinalIgnoreCase
    );

    // Act
    var message = new Message("body", headers);

    // Assert
    message.Headers.Should().ContainKey("X-Tenant-Id");
    message.Headers["X-Tags"].Length.Should().Be(2);
    message.Headers["X-Tags"].Should().Contain("tag1").And.Contain("tag2");
  }
}