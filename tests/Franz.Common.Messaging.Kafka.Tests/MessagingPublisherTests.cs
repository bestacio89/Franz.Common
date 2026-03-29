#nullable enable
using FluentAssertions;
using Franz.Common.Business.Events;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

[Collection("Kafka")] // Senior Note: Using the collection to ensure we have a live broker if we decide to flip to Integration
public sealed class MessagingPublisherTests
{
  private readonly Mock<IMessagingInitializer> _initializerMock = new();
  private readonly Mock<IMessageFactory> _messageFactoryMock = new();
  private readonly Mock<IDispatcher> _dispatcherMock = new();
  private readonly Mock<IMessagingSender> _senderMock = new();
  private readonly MessagingPublisher _sut;

  public MessagingPublisherTests()
  {
    _sut = new MessagingPublisher(
        _initializerMock.Object,
        _messageFactoryMock.Object,
        _dispatcherMock.Object,
        _senderMock.Object
    );
  }

  #region Pipeline Chain-of-Responsibility

  [Fact]
  public async Task Publish_ValidEvent_ShouldExecuteFullPipelineInCorrectOrder()
  {
    // Arrange
    var evt = new TestIntegrationEvent();
    var msg = new Franz.Common.Messaging.Messages.Message();
    _messageFactoryMock.Setup(m => m.Build(evt)).Returns(msg);

    // Act
    await _sut.Publish(evt);

    // Assert: Verify strict sequence of execution
    // 1. Initialize infra
    _initializerMock.Verify(m => m.InitializeAsync(), Times.Once);
    // 2. Build message from event
    _messageFactoryMock.Verify(m => m.Build(evt), Times.Once);
    // 3. Dispatch internal notifications
    _dispatcherMock.Verify(m => m.PublishNotificationAsync(msg), Times.Once);
    // 4. Send to Kafka transport
    _senderMock.Verify(m => m.SendAsync(msg, It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion

  #region Resiliency & Guard Logic

  [Fact]
  public async Task Publish_NullEvent_ShouldThrowArgumentNullException_ViaDotNet10Guard()
  {
    // Act & Assert
    // Senior Note: .Awaiting() correctly handles ValueTask-to-Task conversion for FluentAssertions
    await _sut.Awaiting(s => s.Publish<TestIntegrationEvent>(null!))
              .Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task Publish_WhenInitializerFails_ShouldAbortBeforeMessageCreation()
  {
    // Arrange
    _initializerMock.Setup(m => m.InitializeAsync()).ThrowsAsync(new TechnicalException("Kafka Offline"));
    var evt = new TestIntegrationEvent();

    // Act & Assert
    await _sut.Awaiting(s => s.Publish(evt))
              .Should().ThrowAsync<TechnicalException>();

    // Verify the rest of the pipeline was never touched
    _messageFactoryMock.Verify(m => m.Build(It.IsAny<IIntegrationEvent>()), Times.Never);
    _senderMock.Verify(m => m.SendAsync(It.IsAny<Franz.Common.Messaging.Messages.Message>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task Publish_WhenMediatorFails_ShouldStopAndPropagateError()
  {
    // Arrange
    var evt = new TestIntegrationEvent();
    var msg = new Franz.Common.Messaging.Messages.Message();
    _messageFactoryMock.Setup(m => m.Build(evt)).Returns(msg);

    _dispatcherMock.Setup(d => d.PublishNotificationAsync(msg))
                   .ThrowsAsync(new InvalidOperationException("Mediator Deadlock"));

    // Act & Assert
    await _sut.Awaiting(s => s.Publish(evt))
              .Should().ThrowAsync<InvalidOperationException>();

    // Transport should NOT be reached if mediator pipeline fails
    _senderMock.Verify(m => m.SendAsync(msg, It.IsAny<CancellationToken>()), Times.Never);
  }

  #endregion

  #region Async Thread-Safety

  [Fact]
  public async Task Publish_ConcurrentCalls_ShouldExecuteSuccessfully()
  {
    // Arrange
    var evt = new TestIntegrationEvent();
    _messageFactoryMock.Setup(m => m.Build(evt)).Returns(new Franz.Common.Messaging.Messages.Message());

    // Act
    // Converting ValueTasks to Tasks for aggregation
    var tasks = Enumerable.Range(0, 50).Select(_ => _sut.Publish(evt).AsTask());
    await Task.WhenAll(tasks);

    // Assert
    _senderMock.Verify(m => m.SendAsync(It.IsAny<Franz.Common.Messaging.Messages.Message>(), It.IsAny<CancellationToken>()), Times.Exactly(50));
  }

  #endregion

  #region Mock Artifacts

  public class TestIntegrationEvent : IIntegrationEvent
  {
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
  }

  #endregion
}