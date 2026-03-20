#nullable enable
using System.Threading.Tasks;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Kafka;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  public class MessagingPublisherTests
  {
    private readonly Mock<IMessagingInitializer> _initializerMock;
    private readonly Mock<IMessageFactory> _factoryMock;
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly Mock<IMessagingSender> _senderMock;
    private readonly MessagingPublisher _publisher;

    public MessagingPublisherTests()
    {
      _initializerMock = new Mock<IMessagingInitializer>(MockBehavior.Strict);
      _factoryMock = new Mock<IMessageFactory>(MockBehavior.Strict);
      _dispatcherMock = new Mock<IDispatcher>(MockBehavior.Strict);
      _senderMock = new Mock<IMessagingSender>(MockBehavior.Strict);

      _publisher = new MessagingPublisher(
          _initializerMock.Object,
          _factoryMock.Object,
          _dispatcherMock.Object,
          _senderMock.Object
      );
    }

    [Fact]
    public async Task Publish_NullEvent_ThrowsTechnicalException()
    {
      await Assert.ThrowsAsync<TechnicalException>(() => _publisher.Publish<IIntegrationEvent>(null!));
    }

    [Fact]
    public async Task Publish_ValidEvent_CallsAllDependenciesInOrder()
    {
      // Arrange
      var integrationEventMock = new Mock<IIntegrationEvent>();
      var messageMock = new Franz.Common.Messaging.Messages.Message("payload");

      _initializerMock.Setup(i => i.Initialize());
      _factoryMock.Setup(f => f.Build(integrationEventMock.Object)).Returns(messageMock);
      _dispatcherMock.Setup(d => d.PublishNotificationAsync(messageMock)).Returns(Task.CompletedTask);
      _senderMock.Setup(s => s.SendAsync(messageMock)).Returns(Task.CompletedTask);

      // Act
      await _publisher.Publish(integrationEventMock.Object);

      // Assert
      _initializerMock.Verify(i => i.Initialize(), Times.Once);
      _factoryMock.Verify(f => f.Build(integrationEventMock.Object), Times.Once);
      _dispatcherMock.Verify(d => d.PublishNotificationAsync(messageMock), Times.Once);
      _senderMock.Verify(s => s.SendAsync(messageMock), Times.Once);
    }

    [Fact]
    public async Task Publish_DispatchThrows_DoesNotThrowPublisher()
    {
      // Arrange
      var integrationEventMock = new Mock<IIntegrationEvent>();
      var messageMock = new Franz.Common.Messaging.Messages.Message("payload");

      _initializerMock.Setup(i => i.Initialize());
      _factoryMock.Setup(f => f.Build(integrationEventMock.Object)).Returns(messageMock);

      // Dispatcher throws, but publisher should not fail
      _dispatcherMock.Setup(d => d.PublishNotificationAsync(messageMock))
                     .ThrowsAsync(new System.Exception("Dispatcher failure"));

      _senderMock.Setup(s => s.SendAsync(messageMock)).Returns(Task.CompletedTask);

      // Act & Assert
      await _publisher.Publish(integrationEventMock.Object);

      // Verify all called
      _initializerMock.Verify(i => i.Initialize(), Times.Once);
      _factoryMock.Verify(f => f.Build(integrationEventMock.Object), Times.Once);
      _dispatcherMock.Verify(d => d.PublishNotificationAsync(messageMock), Times.Once);
      _senderMock.Verify(s => s.SendAsync(messageMock), Times.Once);
    }
  }
}