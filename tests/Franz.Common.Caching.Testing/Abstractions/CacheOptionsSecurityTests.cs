using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    CacheOptions.SectionName.Should().Be("Franz:Caching");
    CacheOptions.ConfigurationSectionName.Should().Be("Caching");
  }

  [Fact]
  public void Registration_Should_RejectInvalidKeyPrefix()
  {
    var services = new ServiceCollection();
    services.AddFranzMemoryCaching(options => options.KeyPrefix = "invalid prefix");
    using var provider = services.BuildServiceProvider();

    var action = () => provider.GetRequiredService<IOptions<CacheOptions>>().Value;

    action.Should().Throw<OptionsValidationException>();
  }

  [Fact]
  public async Task PerCallOptions_Should_RejectInvalidExpirationBeforeRunningFactory()
  {
    var services = new ServiceCollection();
    services.AddFranzMemoryCaching();
    using var provider = services.BuildServiceProvider();
    var cache = provider.GetRequiredService<ICacheProvider>();
    var factoryCalls = 0;

    var action = () => cache.GetOrSetAsync(
        "invalid-per-call-options",
        _ =>
        {
          factoryCalls++;
          return Task.FromResult("value");
        },
        new CacheOptions
        {
          DefaultAbsoluteExpiration = TimeSpan.FromSeconds(-1)
        });

    await action.Should().ThrowAsync<ArgumentException>();
    factoryCalls.Should().Be(0);
  }

  [Fact]
  public void Registration_Should_RejectNegativeEstimatedSize()
  {
    var services = new ServiceCollection();
    services.AddFranzMemoryCaching(options => options.DefaultEstimatedSizeInBytes = -1);
    using var provider = services.BuildServiceProvider();

    var action = () => provider.GetRequiredService<IOptions<CacheOptions>>().Value;

    action.Should().Throw<OptionsValidationException>();
  }
}
