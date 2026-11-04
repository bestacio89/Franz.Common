using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Xunit;

namespace Franz.Common.Messaging.Tests;

public sealed class MessagingPolicyTests
{
  [Fact]
  public void MessageFailurePolicy_ShouldExposeBrokerNeutralDefaults()
  {
    var policy = MessageFailurePolicy.Default;

    policy.MaxRetryAttempts.Should().Be(3);
    policy.RetryBackoff.Should().Be(TimeSpan.FromMilliseconds(500));
    policy.DeadLetterOnExhaustion.Should().BeTrue();
  }

  [Fact]
  public void MessageFailurePolicy_ShouldPreserveExplicitValues()
  {
    var policy = new MessageFailurePolicy(
        maxRetryAttempts: 5,
        retryBackoff: TimeSpan.FromSeconds(2),
        deadLetterOnExhaustion: false);

    policy.MaxRetryAttempts.Should().Be(5);
    policy.RetryBackoff.Should().Be(TimeSpan.FromSeconds(2));
    policy.DeadLetterOnExhaustion.Should().BeFalse();
  }

  [Theory]
  [InlineData(-1)]
  public void MessageFailurePolicy_ShouldRejectNegativeRetryAttempts(int attempts)
  {
    var action = () => new MessageFailurePolicy(maxRetryAttempts: attempts);

    action.Should().Throw<ArgumentOutOfRangeException>()
        .WithParameterName("maxRetryAttempts");
  }

  [Fact]
  public void MessageFailurePolicy_ShouldRejectNegativeBackoff()
  {
    var action = () => new MessageFailurePolicy(retryBackoff: TimeSpan.FromMilliseconds(-1));

    action.Should().Throw<ArgumentOutOfRangeException>()
        .WithParameterName("retryBackoff");
  }

  [Fact]
  public void MessageOrderingStrategy_ShouldExposeOnlyTransportNeutralValues()
  {
    Enum.GetNames<MessageOrderingStrategy>().Should().Equal(
        "None",
        "MessageId",
        "CorrelationId",
        "AggregateId");
  }
}
