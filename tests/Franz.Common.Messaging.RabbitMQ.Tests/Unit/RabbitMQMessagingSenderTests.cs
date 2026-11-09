#nullable enable

using FluentAssertions;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Unit;

public sealed class RabbitMQMessagingSenderTests
{
  [Fact]
  public async Task SendAsync_MapsMessageIdentityAndCorrelationToRabbitProperties()
  {
    var channelPool = new Mock<IChannelPool>();
    var handler = new Mock<IMessageHandler>();
    var channel = new Mock<IChannel>();
    var messageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var message = new Message("{}")
    {
      Id = messageId,
      CorrelationId = correlationId
    };
    BasicProperties? publishedProperties = null;

    channelPool
        .Setup(pool => pool.GetAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(channel.Object);

    channel
        .Setup(candidate => candidate.BasicPublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()))
        .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
            (_, _, _, properties, _, _) => publishedProperties = properties)
        .Returns(ValueTask.CompletedTask);

    var sender = new RabbitMQMessagingSender(
        channelPool.Object,
        handler.Object,
        transaction: null,
        Options.Create(new RabbitMQMessagingOptions()));

    await sender.SendAsync(message);

    publishedProperties.Should().NotBeNull();
    publishedProperties!.MessageId.Should().Be(messageId.ToString("D"));
    publishedProperties.CorrelationId.Should().Be(correlationId.ToString("D"));
    channelPool.Verify(pool => pool.Return(channel.Object), Times.Once);
  }
}
