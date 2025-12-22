using FluentAssertions;
using Franz.Common.Caching.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Options;

public sealed class MediatorCachingOptionsTests
{
  [Fact]
  public void Defaults_Should_Be_Correct()
  {
    var options = new MediatorCachingOptions();

    options.Enabled.Should().BeTrue();
    options.DefaultTtl.Should().Be(TimeSpan.FromMinutes(5));
    options.ShouldCache.Should().BeNull();
    options.TtlSelector.Should().BeNull();
    options.LogHitLevel.Should().Be(LogLevel.Debug);
    options.LogMissLevel.Should().Be(LogLevel.Information);
  }

  [Fact]
  public void Properties_Should_Be_Settable()
  {
    var options = new MediatorCachingOptions
    {
      Enabled = false,
      DefaultTtl = TimeSpan.FromSeconds(10),
      ShouldCache = _ => false,
      TtlSelector = _ => TimeSpan.FromSeconds(1),
      LogHitLevel = LogLevel.Trace,
      LogMissLevel = LogLevel.Warning
    };

    options.Enabled.Should().BeFalse();
    options.DefaultTtl.Should().Be(TimeSpan.FromSeconds(10));
    options.ShouldCache.Should().NotBeNull();
    options.TtlSelector.Should().NotBeNull();
  }
}
