#nullable enable
using FluentAssertions;
using Franz.Common.Business.Events;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Messages;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

[Collection("Kafka")]
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

  #region Pipeline Execution Order

  [Fact]
  public async Task Publish_ShouldExecutePipelineInOrder()
  {
    var evt = new TestIntegrationEvent();
    var msg = new Message();

    var sequence = new MockSequence();

    _initializerMock.InSequence(sequence)
    .Setup(m => m.InitializeAsync())
    .Returns(ValueTask.CompletedTask);

    _messageFactoryMock.InSequence(sequence)
        .Setup(m => m.Build(evt))
        .Returns(msg);

    _dispatcherMock.InSequence(sequence)
        .Setup(d => d.PublishNotificationAsync(msg))
        .Returns(Task.CompletedTask);

    _senderMock.InSequence(sequence)
    .Setup(s => s.SendAsync(msg, It.IsAny<CancellationToken>()))
    .Returns(ValueTask.CompletedTask);
    await _sut.Publish(evt);

    _initializerMock.VerifyAll();
    _messageFactoryMock.VerifyAll();
    _dispatcherMock.VerifyAll();
    _senderMock.VerifyAll();
  }

  #endregion

  #region Guard & Failure Behavior

  [Fact]
  public async Task Publish_NullEvent_ShouldThrowArgumentNullException()
  {
    await _sut.Awaiting(s => s.Publish<TestIntegrationEvent>(null!))
              .Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task Publish_WhenInitializerFails_ShouldStopPipeline()
  {
    _initializerMock.Setup(m => m.InitializeAsync())
        .ThrowsAsync(new TechnicalException("Kafka Offline"));

    var evt = new TestIntegrationEvent();

    await _sut.Awaiting(s => s.Publish(evt))
              .Should().ThrowAsync<TechnicalException>();

    _messageFactoryMock.Verify(m => m.Build(It.IsAny<IIntegrationEvent>()), Times.Never);
    _dispatcherMock.Verify(m => m.PublishNotificationAsync(It.IsAny<Message>()), Times.Never);
    _senderMock.Verify(m => m.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task Publish_WhenDispatcherFails_ShouldStopBeforeSending()
  {
    var evt = new TestIntegrationEvent();
    var msg = new Message();

    _messageFactoryMock.Setup(m => m.Build(evt)).Returns(msg);

    _dispatcherMock.Setup(d => d.PublishNotificationAsync(msg))
        .ThrowsAsync(new InvalidOperationException("Mediator Failure"));

    await _sut.Awaiting(s => s.Publish(evt))
              .Should().ThrowAsync<InvalidOperationException>();

    _senderMock.Verify(m => m.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  #endregion

  #region Concurrency Safety

  [Fact]
  public async Task Publish_ConcurrentCalls_ShouldBeSafe()
  {
    var evt = new TestIntegrationEvent();

    _messageFactoryMock
        .Setup(m => m.Build(It.IsAny<IIntegrationEvent>()))
        .Returns(() => new Message());

    var tasks = Enumerable.Range(0, 50)
        .Select(_ => _sut.Publish(evt).AsTask());

    await Task.WhenAll(tasks);

    _senderMock.Verify(
        m => m.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
        Times.Exactly(50));
  }

  #endregion

  public class TestIntegrationEvent : IIntegrationEvent
  {
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
  }
}