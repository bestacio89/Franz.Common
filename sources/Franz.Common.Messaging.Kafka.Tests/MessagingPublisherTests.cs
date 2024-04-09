using Confluent.Kafka;
using Franz.Common.Business.Events;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Testing;
using Moq;
using System.Threading;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  public class MessagingPublisherTests
  {
    private readonly Mock<IProducer<string, byte[]>> _producerMock;
    private readonly Mock<IMessagingInitializer> _messagingInitializerMock;
    private readonly Mock<IMessageFactory> _messageFactoryMock;
    private readonly Mock<IMessageHandler> _messageHandlerMock;
    private readonly MessagingPublisher _publisher;

    public MessagingPublisherTests()
    {
      _producerMock = new Mock<IProducer<string, byte[]>>();
      _messagingInitializerMock = new Mock<IMessagingInitializer>();
      _messageFactoryMock = new Mock<IMessageFactory>();
      _messageHandlerMock = new Mock<IMessageHandler>();

      _publisher = new MessagingPublisher(
          _producerMock.Object,
          _messagingInitializerMock.Object,
          _messageFactoryMock.Object,
          _messageHandlerMock.Object);
    }

    [Fact]
    public void Publish_WithValidIntegrationEvent_CallsInitialize()
    {
      var integrationEvent = new TestIntegrationEvent();

      _publisher.Publish(integrationEvent);

      _messagingInitializerMock.Verify(m => m.Initialize(), Times.Once);
    }

    [Fact]
    public void Publish_WithValidIntegrationEvent_CallsBuild()
    {
      var integrationEvent = new TestIntegrationEvent();

      _publisher.Publish(integrationEvent);

      _messageFactoryMock.Verify(m => m.Build(integrationEvent), Times.Once);
    }

    [Fact]
    public void Publish_WithValidIntegrationEvent_CallsProcess()
    {
      var integrationEvent = new TestIntegrationEvent();
      var message = new Message();
      _messageFactoryMock.Setup(m => m.Build(integrationEvent)).Returns(message);

       _publisher.Publish(integrationEvent);

      _messageHandlerMock.Verify(m => m.Process(message), Times.Once);
    }

    [Fact]
    public void Publish_WithValidIntegrationEvent_CallsProduce()
    {
      var integrationEvent = new TestIntegrationEvent();
      var message = new Message();
      _messageFactoryMock.Setup(m => m.Build(integrationEvent)).Returns(message);
      _producerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new DeliveryResult<string, byte[]>());

      _publisher.Publish(integrationEvent);

      _producerMock.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Publish_WithValidIntegrationEvent_CallsFlush()
    {
      var integrationEvent = new TestIntegrationEvent();
      var message = new Message();
      _messageFactoryMock.Setup(m => m.Build(integrationEvent)).Returns(message);
      _producerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new DeliveryResult<string, byte[]>());

      _publisher.Publish(integrationEvent);

      _producerMock.Verify(p => p.Flush(It.IsAny<CancellationToken>()), Times.Once);
    }

 

    private class TestIntegrationEvent : BaseEvent, IIntegrationEvent
    {
    }
  }
}
