using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Testing;
using Franz.Common.Business.Events;

namespace Franz.Common.Messaging.Kafka.Tests
{
  [TestFixture]
  public class MessagingPublisherTests : UnitTest
  {
    private MessagingPublisher _publisher;
    private Mock<IProducer<string, byte[]>> _producerMock;
    private Mock<IMessagingInitializer> _messagingInitializerMock;
    private Mock<IMessageFactory> _messageFactoryMock;
    private Mock<IMessageHandler> _messageHandlerMock;

    [SetUp]
    public void SetUp()
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

    [Test]
    public void Publish_WithValidIntegrationEvent_CallsInitialize()
    {
      var integrationEvent = new TestIntegrationEvent();

      _publisher.Publish(integrationEvent);

      _messagingInitializerMock.Verify(m => m.Initialize(), Times.Once);
    }

    [Test]
    public void Publish_WithValidIntegrationEvent_CallsBuild()
    {
      var integrationEvent = new TestIntegrationEvent();

      _publisher.Publish(integrationEvent);

      _messageFactoryMock.Verify(m => m.Build(integrationEvent), Times.Once);
    }

    [Test]
    public void Publish_WithValidIntegrationEvent_CallsProcess()
    {
      var integrationEvent = new TestIntegrationEvent();
      var message = new Message();
      _messageFactoryMock.Setup(m => m.Build(integrationEvent)).Returns(message);

      _publisher.Publish(integrationEvent);

      _messageHandlerMock.Verify(m => m.Process(message), Times.Once);
    }

    [Test]
    public void Publish_WithValidIntegrationEvent_CallsProduceAsync()
    {
      var integrationEvent = new TestIntegrationEvent();
      var message = new Message();
      _messageFactoryMock.Setup(m => m.Build(integrationEvent)).Returns(message);
      _producerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new DeliveryResult<string, byte[]>());

      _publisher.Publish(integrationEvent);

      _producerMock.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
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

    [Test]
    public void Publish_WithInvalidIntegrationEvent_ThrowsException()
    {
      _producerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception());

      Assert.Throws<Exception>(() => _publisher.Publish(new TestIntegrationEvent()));
    }

    private class TestIntegrationEvent : IIntegrationEvent
    {
    }
  }
}
