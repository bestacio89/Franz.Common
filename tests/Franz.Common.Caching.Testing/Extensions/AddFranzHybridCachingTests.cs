using FluentAssertions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Caching.Tests.Extensions;

public sealed class AddFranzHybridCachingTests
{
  [Fact]
  public async Task SetAsync_Should_Preserve_Null_As_A_Cached_Value()
  {
    var services = new ServiceCollection();
    services.AddFranzHybridCaching(options =>
    {
      options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
      options.DefaultSlidingExpiration = TimeSpan.FromMinutes(1);
    });

    using var serviceProvider = services.BuildServiceProvider();
    var cache = serviceProvider.GetRequiredService<ICacheProvider>();

    var writer = (ICacheWriter)cache;
    await writer.SetAsync<string?>("nullable-entry", null);

    var factoryCalls = 0;
    var result = await cache.GetOrSetAsync<string?>("nullable-entry", _ =>
    {
      factoryCalls++;
      return Task.FromResult<string?>("factory");
    });

    result.IsHit.Should().BeTrue();
    result.Value.Should().BeNull();
    factoryCalls.Should().Be(0);
  }

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

  [Fact]
  public void Should_Replace_ReflectionBased_DefaultSerializerFactory()
  {
    var services = new ServiceCollection();
    services.AddFranzHybridCaching();

    using var provider = services.BuildServiceProvider();

    provider.GetServices<IHybridCacheSerializerFactory>()
        .Should().ContainSingle();
  }
}
