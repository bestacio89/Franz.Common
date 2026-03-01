using FluentAssertions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.Extensions.Primitives;
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
    message.Headers.Add("X-Tenant-Id", "tenant-1");
    message.Headers.Add("X-Empty", "   "); // Should be stripped
    message.Properties["InternalFlag"] = true;

    // Act
    var stored = message.ToStored();

    // Assert
    stored.Id.Should().Be(originalId);
    stored.Body.Should().Be(message.Body);
    stored.Headers.Should().ContainKey("X-Tenant-Id");
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
      Headers = new Dictionary<string, string[]>
            {
                { "X-Roles", new[] { "Admin", "User" } }
            }
    };

    // Act
    var message = stored.ToMessage();

    // Assert
    message.Headers.TryGetValue("X-Roles", out var values).Should().BeTrue();
    values.Count.Should().Be(2);
    values.Should().Contain("Admin").And.Contain("User");
  }

  [Fact]
  public void StoredMessage_Constructor_ShouldDefaultToGuidV7()
  {
    // Act
    var stored = new StoredMessage();

    // Assert
    // Check version 7 (the 13th character in a hyphenated GUID string)
    stored.Id.ToString()[14].Should().Be('7');
  }
}