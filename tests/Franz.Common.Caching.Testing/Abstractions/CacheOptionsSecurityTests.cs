using FluentAssertions;
using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Tests;

public sealed class CacheOptionsSecurityTests
{
  [Fact]
  public void ToString_Should_RedactRedisConnectionString()
  {
    var options = new CacheOptions
    {
      ConnectionString = "redis.internal:6379,password=super-secret"
    };

    var formatted = options.ToString();

    formatted.Should().Contain("ConnectionString = [REDACTED]");
    formatted.Should().NotContain("super-secret");
  }

  [Fact]
  public void SectionName_Should_Match_ConfigurationBindingSection()
  {
    CacheOptions.SectionName.Should().Be("Caching");
  }
}
