using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Franz.Common.Messaging.Kafka.Hosting;
using Franz.Common.Reflection;
using Franz.Common.Testing;

namespace Franz.Common.Messaging.Kafka.Tests.Hosting
{
  public class KafkaListenerTests
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

    [Fact]
    public void Listen_ShouldSubscribeToTopic()
    {
      _listener.Listen();

      _consumerMock.Verify(x => x.Subscribe(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Listen_ShouldInvokeReceivedEvent()
    {
      bool received = false;
      _listener.Received += (sender, args) => received = true;

      _consumerMock.Setup(x => x.Consume(It.IsAny<TimeSpan>()))
          .Returns(new ConsumeResult<Ignore, string>() { Message = new Message<Ignore, string> { Value = "test message" } });

      _listener.Listen();

      Assert.True(received);
    }

    [Fact]
    public async Task Listen_ShouldLogErrorOnConsumeException()
    {
      _consumerMock.Setup(x => x.Consume(It.IsAny<TimeSpan>()))
          .Throws(new ConsumeException(new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.Unknown, "error")));

      _listener.Listen();

      _loggerMock.Verify(x => x.LogError(It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task StopListen_ShouldUnsubscribeFromTopic()
    {
       _listener.StopListen();

      _consumerMock.Verify(x => x.Unsubscribe(), Times.Once);
    }
  }
}
