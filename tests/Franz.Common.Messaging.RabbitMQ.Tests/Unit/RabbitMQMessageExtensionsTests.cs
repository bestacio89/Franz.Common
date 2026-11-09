using FluentAssertions;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Unit;

public sealed class RabbitMQMessageExtensionsTests
{
  [Fact]
  public void ToBasicProperties_RejectsEmptyMessageIdentity()
  {
    var message = new Message("payload")
    {
      Id = Guid.Empty
    };

    var act = () => message.ToBasicProperties();

    act.Should().Throw<PermanentMessagingException>()
        .WithMessage("*non-empty message identity*");
  }
}
