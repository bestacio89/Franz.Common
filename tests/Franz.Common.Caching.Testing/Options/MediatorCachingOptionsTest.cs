using FluentAssertions;
using Franz.Common.Caching.Options;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Franz.Common.Caching.Tests.Options;

public sealed class MediatorCachingOptionsTests
{
  [Fact]
  public void Defaults_Should_Be_Correct()
  {
    // Act
    var options = new MediatorCachingOptions();

    // Assert
    options.Enabled.Should().BeTrue();
    options.BypassAll.Should().BeFalse();
    options.DefaultTtl.Should().Be(TimeSpan.FromMinutes(5));
    options.DefaultSlidingExpiration.Should().Be(TimeSpan.FromMinutes(2));
    options.LogHitLevel.Should().Be(LogLevel.Debug);
    options.LogMissLevel.Should().Be(LogLevel.Information);
  }

  [Fact]
  public void Properties_Should_Be_Settable()
  {
    // Act
    var options = new MediatorCachingOptions
    {
      Enabled = false,
      BypassAll = true,
      DefaultTtl = TimeSpan.FromSeconds(10),
      DefaultSlidingExpiration = TimeSpan.FromSeconds(5),
      LogHitLevel = LogLevel.Trace,
      LogMissLevel = LogLevel.Warning
    };

    // Assert
    options.Enabled.Should().BeFalse();
    options.BypassAll.Should().BeTrue();
    options.DefaultTtl.Should().Be(TimeSpan.FromSeconds(10));
    options.DefaultSlidingExpiration.Should().Be(TimeSpan.FromSeconds(5));
    options.LogHitLevel.Should().Be(LogLevel.Trace);
    options.LogMissLevel.Should().Be(LogLevel.Warning);
  }
}