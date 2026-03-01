using FluentAssertions;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
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
    // Guid v7 check: version is in the 48-51 bits (approx)
    message.Id.ToString()[14].Should().Be('7');
  }

  [Fact]
  public void CorrelationId_WhenNotProvided_ShouldGenerateNewV7AndStoreInProperties()
  {
    // Arrange
    var message = new Message();

    // Act
    var cid = message.CorrelationId;

    // Assert
    cid.Should().NotBeEmpty();
    message.Properties.Should().ContainKey(nameof(message.CorrelationId));
    message.Properties[nameof(message.CorrelationId)].Should().Be(cid);
  }

  [Fact]
  public void CorrelationId_WhenExistsInPropertiesAsString_ShouldParseAndReturn()
  {
    // Arrange
    var message = new Message();
    var existingCid = Guid.NewGuid();
    message.Properties[nameof(Message.CorrelationId)] = existingCid.ToString();

    // Act & Assert
    message.CorrelationId.Should().Be(existingCid);
  }

  [Fact]
  public void SyncCorrelationFromHeaders_WhenHeaderExists_ShouldUpdateCorrelationId()
  {
    // Arrange
    var cid = Guid.NewGuid();
    var headers = new MessageHeaders();
    headers.Add("correlation-id", cid.ToString());

    // Act
    var message = new Message("payload", headers);

    // Assert
    message.CorrelationId.Should().Be(cid);
    message.Properties[nameof(Message.CorrelationId)].Should().Be(cid);
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
    var dict = new Dictionary<string, IReadOnlyCollection<string>>
        {
            { "X-Tenant-Id", new[] { "tenant-1" } },
            { "X-Tags", new[] { "tag1", "tag2" } }
        };

    // Act
    var message = new Message("body", dict);

    // Assert
    message.Headers.Should().ContainKey("X-Tenant-Id");
    message.Headers["X-Tags"].Should().HaveCount(2);
  }
}