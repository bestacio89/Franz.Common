using FluentAssertions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Caching.Tests.Extensions;

public sealed class AddFranzHybridCachingTests
{
  [Fact]
  public void Should_Register_HybridCacheProvider()
  {
    var services = new ServiceCollection();
    services.AddFranzHybridCaching(options =>
    {
      options.KeyPrefix = "hybrid:";
      options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
    });

    using var provider = services.BuildServiceProvider();

    provider.GetRequiredService<ICacheProvider>()
        .Should().BeOfType<HybridCacheProvider>();
  }
}
