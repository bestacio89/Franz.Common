#nullable enable

using System;
using System.Text.Json;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Kafka.Serialisation;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Serialization
{
  // Minimal subclass for testing if needed
  public class TestMessage : StoredMessage
  {
    // Only add extra fields for JSON payload testing
    public string Content { get; set; } = "";
  }

  public class JsonMessageDeserializerTests
  {
    private readonly JsonMessageDeserializer<TestMessage> _deserializer;

    public JsonMessageDeserializerTests()
    {
      // Use default Franz JSON options
      _deserializer = new JsonMessageDeserializer<TestMessage>();
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsMessage()
    {
      // Arrange
      var json = "{\"Content\":\"Hello World\",\"Body\":\"payload\",\"Id\":\"00000000-0000-0000-0000-000000000001\",\"CorrelationId\":\"00000000-0000-0000-0000-000000000002\"}";

      // Act
      var result = _deserializer.Deserialize(json);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("Hello World", result.Content);
      Assert.Equal("payload", result.Body);
      Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), result.Id);
      Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), result.CorrelationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Deserialize_EmptyOrNull_ThrowsTechnicalException(string input)
    {
      // Act & Assert
      var ex = Assert.Throws<TechnicalException>(() => _deserializer.Deserialize(input));
      Assert.Contains("input message is null or empty", ex.Message);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsTechnicalException()
    {
      // Arrange
      var invalidJson = "{ this is not valid json }";

      // Act & Assert
      var ex = Assert.Throws<TechnicalException>(() => _deserializer.Deserialize(invalidJson));
      Assert.Contains("Failed to deserialize", ex.Message);
    }

    [Fact]
    public void Deserialize_JsonWithMissingOptionalFields_ReturnsDefaultValues()
    {
      // Arrange: only Body provided, other fields omitted
      var json = "{\"Body\":\"payload\"}";

      // Act
      var result = _deserializer.Deserialize(json);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("payload", result.Body);
      Assert.Equal(Guid.Empty, default(Guid)); // Id will default to Guid.NewGuid() in constructor
      Assert.Equal(Guid.Empty, result.CorrelationId); // default Guid for missing
      Assert.Equal("", result.Content); // default string
    }

    [Fact]
    public void Deserialize_JsonLiteralNull_ThrowsTechnicalException()
    {
      // Arrange
      var json = "null";

      // Act & Assert
      var ex = Assert.Throws<TechnicalException>(() => _deserializer.Deserialize(json));
      Assert.Contains("Deserialization returned null", ex.Message);
    }
  }
}