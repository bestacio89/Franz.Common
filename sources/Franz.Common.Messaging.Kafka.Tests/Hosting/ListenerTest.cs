using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Franz.Common.Reflection;
using Franz.Common.Testing;
using Moq;
using System;
using System.Linq;
using NUnit.Framework;
using Franz.Common.Messaging.Kafka.Hosting;
using System.Text;

namespace Franz.Common.Messaging.Kafka.Tests.Hosting
{
  [TestFixture]
  public class KafkaListenerTests : UnitTest
  {
    private readonly Mock<IConsumer<Ignore, string>> _consumerMock;
    private readonly Mock<IAssemblyAccessor> _assemblyAccessorMock;
    private readonly Mock<ILogger<KafkaListener>> _loggerMock;
    private readonly KafkaListener _listener;

    public KafkaListenerTests()
    {
      _consumerMock = new Mock<IConsumer<Ignore, string>>();
      _assemblyAccessorMock = new Mock<IAssemblyAccessor>();
      _loggerMock = new Mock<ILogger<KafkaListener>>();
      _listener = new KafkaListener(_consumerMock.Object, _assemblyAccessorMock.Object, _loggerMock.Object);
    }

    [Test]
    public void Listen_ShouldSubscribeToTopic()
    {
      _listener.Listen();
      _consumerMock.Verify(x => x.Subscribe(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void Listen_ShouldInvokeReceivedEvent()
    {
      var received = false;
      _listener.Received += (sender, args) => { received = true; };
      _consumerMock.Setup(x => x.Consume(It.IsAny<TimeSpan>()))
           .Returns(new ConsumeResult<Ignore, string>() { Message = new Message<Ignore, string> { Value = "test message" } });


      _listener.Listen();

      Assert.True(received);
    }

    [Test]
    public void Listen_ShouldLogErrorOnConsumeException()
    {
      _consumerMock.Setup(x => x.Consume(It.IsAny<TimeSpan>()))
          .Throws(new ConsumeException(new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.Unknown, "error")));

      _listener.Listen();

      _loggerMock.Verify(x => x.LogError(It.IsAny<string>(), null), Times.Once);
    }

    [Test]
    public void StopListen_ShouldUnsubscribeFromTopic()
    {
      _listener.StopListen();
      _consumerMock.Verify(x => x.Unsubscribe(), Times.Once);
    }

  }
}
