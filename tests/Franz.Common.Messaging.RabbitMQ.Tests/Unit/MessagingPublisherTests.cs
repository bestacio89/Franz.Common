#nullable enable
using FluentAssertions;
using Franz.Common.Business.Events;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests;

public sealed class RabbitMQMessagingPublisherTests
{
  private readonly Mock<IChannelPool> _channelPoolMock = new();
  private readonly Mock<IMessagingInitializer> _initializerMock = new();
  private readonly Mock<IMessageFactory> _messageFactoryMock = new();
  private readonly Mock<IMessageHandler> _handlerMock = new();
  private readonly Mock<IMessagingTransaction> _transactionMock = new();
  private readonly Mock<IChannel> _channelMock = new();
  private readonly RabbitMQMessagingPublisher _sut;

  public RabbitMQMessagingPublisherTests()
  {
    _channelPoolMock.Setup(p => p.GetAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_channelMock.Object);

    var options = Options.Create(new RabbitMQMessagingOptions
    {
      ExchangeName = "test-exchange",
      DefaultRoutingKey = "test-key"
    });

    _sut = new RabbitMQMessagingPublisher(
        _channelPoolMock.Object,
        _initializerMock.Object,
        _messageFactoryMock.Object,
        _handlerMock.Object,
        options,
        _transactionMock.Object
    );
  }

  #region Pipeline Execution & Resource Management

  [Fact]
  public async Task Publish_ValidEvent_ShouldExecuteFullPipelineAndCommitAsync()
  {
    // Arrange
    var evt = new TestIntegrationEvent();
    var msg = new Message { Body = "{\"key\":\"value\"}" };
    _messageFactoryMock.Setup(m => m.Build(evt)).Returns(msg);

    // Act
    await _sut.Publish(evt);

    // Assert: Verify order and execution
    _initializerMock.Verify(m => m.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    _messageFactoryMock.Verify(m => m.Build(evt), Times.Once);

    // FIX CS1061: Matches the new Task ProcessAsync signature
    _handlerMock.Verify(m => m.ProcessAsync(msg, It.IsAny<CancellationToken>()), Times.Once);

    _transactionMock.Verify(t => t.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);

    // FIX CS0311: Use BasicProperties (concrete type) to satisfy IAmqpHeader constraint
    _channelMock.Verify(c => c.BasicPublishAsync(
        "test-exchange",
        "test-key",
        true,
        It.IsAny<BasicProperties>(),
        It.IsAny<ReadOnlyMemory<byte>>(),
        It.IsAny<CancellationToken>()), Times.Once);

    _transactionMock.Verify(t => t.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);

    _channelPoolMock.Verify(p => p.Return(_channelMock.Object), Times.Once);
  }

  #endregion

  #region Error Handling & Rollback

  [Fact]
  public async Task Publish_WhenTransportThrows_ShouldRollbackAndReturnChannelAsync()
  {
    // Arrange
    var evt = new TestIntegrationEvent();
    _messageFactoryMock.Setup(m => m.Build(evt)).Returns(new Message { Body = "{}" });

    // FIX CS0311: Use BasicProperties here as well
    _channelMock.Setup(c => c.BasicPublishAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<bool>(),
        It.IsAny<BasicProperties>(),
        It.IsAny<ReadOnlyMemory<byte>>(),
        It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Transport Failure"));

    // Act & Assert
    await _sut.Awaiting(s => s.Publish(evt, default))
              .Should().ThrowAsync<Exception>();

    _transactionMock.Verify(t => t.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
    _transactionMock.Verify(t => t.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

    _channelPoolMock.Verify(p => p.Return(_channelMock.Object), Times.Once);
  }

  #endregion

  #region Test Artifacts

  public class TestIntegrationEvent : IIntegrationEvent
  {
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
  }

  #endregion
}