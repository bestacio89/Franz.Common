#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.Delegating;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Unit;

public sealed class RabbitMQMessagingSenderTests
{
  [Fact]
  public async Task SendAsync_WhenMessageBodyIsNull_ShouldReportPermanentFailureBeforeTransportAccess()
  {
    var channelPool = new Mock<IChannelPool>(MockBehavior.Strict);
    var handler = new Mock<IMessageHandler>(MockBehavior.Strict);
    var sender = new RabbitMQMessagingSender(
        channelPool.Object,
        handler.Object,
        transaction: null,
        Options.Create(new RabbitMQMessagingOptions()));

    var message = new Message(body: null);

    var act = () => sender.SendAsync(message).AsTask();

    var error = await act.Should().ThrowAsync<PermanentMessagingException>();
    error.Which.Message.Should().Contain(message.Id.ToString()).And.Contain("no body");
    handler.VerifyNoOtherCalls();
    channelPool.VerifyNoOtherCalls();
  }
}
