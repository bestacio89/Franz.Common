using Franz.Common.Caching.Options;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;

namespace Franz.Common.Integration.Tests.Caching.Abstractions;
public class MediatorCachingOptionsTests
{
  [Fact]
  public void Should_Have_Defaults()
  {
    var o = new MediatorCachingOptions();
    o.Enabled.Should().BeTrue();
    o.DefaultTtl.Should().Be(TimeSpan.FromMinutes(5));
    o.LogHitLevel.Should().Be(LogLevel.Debug);
  }

  [Fact]
  public void ShouldCache_Predicate_Should_Work()
  {
    var o = new MediatorCachingOptions
    {
      ShouldCache = req => req.ToString()!.Contains("cache")
    };

    o.ShouldCache!("cache_this").Should().BeTrue();
    o.ShouldCache!("nope").Should().BeFalse();
  }
}