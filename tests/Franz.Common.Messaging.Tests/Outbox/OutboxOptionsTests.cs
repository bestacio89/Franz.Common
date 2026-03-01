using FluentAssertions;
using Franz.Common.Messaging.Outbox;
using Xunit;

namespace Franz.Common.Messaging.Tests.Outbox;

public class OutboxOptionsTests
{
  [Fact]
  public void OutboxOptions_ShouldHaveSensibleDefaults()
  {
    // Act
    var options = new OutboxOptions();

    // Assert
    options.Enabled.Should().BeFalse(); // Safe by default
    options.PollingInterval.Should().Be(TimeSpan.FromSeconds(5));
    options.MaxRetries.Should().Be(3);
    options.DeadLetterEnabled.Should().BeFalse();
  }

  [Fact]
  public void OutboxOptions_ShouldAllowPropertyModification()
  {
    // Arrange
    var options = new OutboxOptions();
    var customInterval = TimeSpan.FromMinutes(1);

    // Act
    options.Enabled = true;
    options.PollingInterval = customInterval;
    options.MaxRetries = 10;
    options.DeadLetterEnabled = true;

    // Assert
    options.Enabled.Should().BeTrue();
    options.PollingInterval.Should().Be(customInterval);
    options.MaxRetries.Should().Be(10);
    options.DeadLetterEnabled.Should().BeTrue();
  }
}