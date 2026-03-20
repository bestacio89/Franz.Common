#nullable enable

using System;
using System.Text.Json;
using Confluent.Kafka;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  public class KafkaConsumerExtensionsTests
  {
    private readonly Mock<IConsumer<string, string>> _consumerMock;

    public KafkaConsumerExtensionsTests()
    {
      _consumerMock = new Mock<IConsumer<string, string>>(MockBehavior.Strict);
    }

    private ConsumeResult<string, string> CreateConsumeResult(string? value)
    {
      return new ConsumeResult<string, string>
      {
        Message = value is null ? null : new Message<string, string> { Key = "key", Value = value }
      };
    }

    [Fact]
    public void ConsumeCommand_ValidMessage_ReturnsCommand()
    {
      // Arrange
      var msg = new Message("payload") { Kind = MessageKind.Command };
      var json = JsonSerializer.Serialize(msg);

      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(json));

      // Act
      var result = _consumerMock.Object.ConsumeCommand(TimeSpan.FromSeconds(1));

      // Assert
      Assert.NotNull(result);
      Assert.IsAssignableFrom<ICommand>(result);
      Assert.Equal(msg.Body, ((Message)result).Body);
    }

    [Fact]
    public void ConsumeEvent_ValidMessage_ReturnsEvent()
    {
      // Arrange
      var msg = new Message("payload") { Kind = MessageKind.IntegrationEvent };
      var json = JsonSerializer.Serialize(msg);

      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(json));

      // Act
      var result = _consumerMock.Object.ConsumeEvent(TimeSpan.FromSeconds(1));

      // Assert
      Assert.NotNull(result);
      Assert.IsAssignableFrom<IEvent>(result);
      Assert.Equal(msg.Body, ((Message)result).Body);
    }

    [Fact]
    public void ConsumeCommand_NullMessage_ReturnsNull()
    {
      // Arrange
      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(null));

      // Act
      var result = _consumerMock.Object.ConsumeCommand(TimeSpan.FromSeconds(1));

      // Assert
      Assert.Null(result);
    }

    [Fact]
    public void ConsumeEvent_NullMessage_ReturnsNull()
    {
      // Arrange
      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(null));

      // Act
      var result = _consumerMock.Object.ConsumeEvent(TimeSpan.FromSeconds(1));

      // Assert
      Assert.Null(result);
    }

    [Fact]
    public void ConsumeCommand_InvalidJson_ThrowsJsonException()
    {
      // Arrange
      var invalidJson = "{ invalid json }";

      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(invalidJson));

      // Act & Assert
      Assert.Throws<JsonException>(() => _consumerMock.Object.ConsumeCommand(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ConsumeEvent_InvalidJson_ThrowsJsonException()
    {
      // Arrange
      var invalidJson = "{ invalid json }";

      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(invalidJson));

      // Act & Assert
      Assert.Throws<JsonException>(() => _consumerMock.Object.ConsumeEvent(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ConsumeCommand_JsonMissingBody_ReturnsMessageWithNullBody()
    {
      // Arrange: Message serialized without Body
      var msg = new Message() { Kind = MessageKind.Command, Body = null };
      var json = JsonSerializer.Serialize(msg);

      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(json));

      // Act
      var result = _consumerMock.Object.ConsumeCommand(TimeSpan.FromSeconds(1));

      // Assert
      Assert.NotNull(result);
      Assert.IsAssignableFrom<ICommand>(result);
      Assert.Null(((Message)result).Body);
    }

    [Fact]
    public void ConsumeEvent_JsonMissingBody_ReturnsMessageWithNullBody()
    {
      // Arrange: Message serialized without Body
      var msg = new Message() { Kind = MessageKind.IntegrationEvent, Body = null };
      var json = JsonSerializer.Serialize(msg);

      _consumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                   .Returns(CreateConsumeResult(json));

      // Act
      var result = _consumerMock.Object.ConsumeEvent(TimeSpan.FromSeconds(1));

      // Assert
      Assert.NotNull(result);
      Assert.IsAssignableFrom<IEvent>(result);
      Assert.Null(((Message)result).Body);
    }
  }
}