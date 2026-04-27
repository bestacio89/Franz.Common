using FluentAssertions;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Messages;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Tests.Headers;

public class HeaderContextAccessorTests
{
  private readonly Mock<IMessageContextAccessor> _mockAccessor;
  private readonly HeaderContextAccessor _headerAccessor;

  public HeaderContextAccessorTests()
  {
    _mockAccessor = new Mock<IMessageContextAccessor>();
    // Using FranzJson.Default as per the implementation
    _headerAccessor = new HeaderContextAccessor(_mockAccessor.Object);
  }

  private void SetupMessageWithHeaders(Dictionary<string, StringValues> headers)
  {
    var message = new Message("{}");

    if (message.Headers is null)
    {
      throw new InvalidOperationException(
          "Message.Headers must be initialized by Message constructor.");
    }

    foreach (var header in headers)
    {
      // Normalize StringValues -> string[] safely
      var values = header.Value
          .Where(v => v is not null)
          .Select(v => v!)
          .ToArray();

      message.Headers[header.Key] = values;
    }

    var mockContext = new Mock<IMessageContext>();

    mockContext
        .Setup(c => c.Message)
        .Returns(message);

    _mockAccessor
        .Setup(a => a.Current)
        .Returns(mockContext.Object);
  }

  [Fact]
  public void ListAll_WhenContextIsNull_ShouldReturnEmptyEnumerable()
  {
    // Arrange
    _mockAccessor.Setup(a => a.Current).Returns((IMessageContext?)null);

    // Act
    var result = _headerAccessor.ListAll();

    // Assert
    result.Should().BeEmpty();
  }



  [Fact]
  public void TryGetValueGeneric_WithJsonValue_ShouldDeserializeCorrecty()
  {
    // Arrange
    var complexObject = new { Id = 1, Name = "Test" };
    var json = JsonSerializer.Serialize(complexObject, FranzJson.Default);
    SetupMessageWithHeaders(new Dictionary<string, StringValues>
        {
            { "X-Custom-Data", json }
        });

    // Act
    var success = _headerAccessor.TryGetValue<TestDto>("X-Custom-Data", out var value);

    // Assert
    success.Should().BeTrue();
    value.Should().NotBeNull();
    value!.Id.Should().Be(1);
    value.Name.Should().Be("Test");
  }

  [Fact]
  public void TryGetValueGeneric_WhenJsonIsInvalid_ShouldReturnFalse()
  {
    // Arrange
    SetupMessageWithHeaders(new Dictionary<string, StringValues>
        {
            { "X-Invalid-Json", "not-json-at-all" }
        });

    // Act
    var success = _headerAccessor.TryGetValue<TestDto>("X-Invalid-Json", out var value);

    // Assert
    success.Should().BeFalse();
    value.Should().BeNull();
  }

  [Fact]
  public void TryGetValue_WhenKeyMissing_ShouldReturnFalse()
  {
    // Arrange
    SetupMessageWithHeaders(new Dictionary<string, StringValues>());

    // Act
    var success = _headerAccessor.TryGetValue("NonExistent", out _);

    // Assert
    success.Should().BeFalse();
  }

  // DTO for testing generic deserialization
  public class TestDto
  {
    public int Id { get; set; }
    public string? Name { get; set; }
  }
}