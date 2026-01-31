using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MemoryCacheProviderTests
{
  private MemoryCacheProvider CreateProvider()
      => new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));

  [Fact]
  public async Task GetOrSetAsync_Should_Set_And_Get_Value()
  {
    var provider = CreateProvider();

    var value = await provider.GetOrSetAsync(
        "key",
        _ => Task.FromResult("value"),
        new CacheOptions { Expiration = TimeSpan.FromMinutes(1) }
    );

    value.Should().Be("value");

    // Confirm cached value is returned without calling factory again
    var cachedValue = await provider.GetOrSetAsync(
        "key",
        _ => Task.FromResult("wrong"),
        new CacheOptions { Expiration = TimeSpan.FromMinutes(1) }
    );

    cachedValue.Should().Be("value");
  }

  [Fact]
  public async Task RemoveAsync_Should_Delete_Cached_Value()
  {
    var provider = CreateProvider();

    await provider.GetOrSetAsync("temp", _ => Task.FromResult(42), new CacheOptions { Expiration = TimeSpan.FromMinutes(1) });
    await provider.RemoveAsync("temp");

    var result = await provider.GetOrSetAsync<int?>(
        "temp",
        _ => Task.FromResult<int?>(null),
        new CacheOptions { Expiration = TimeSpan.FromMinutes(1) }
    );

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Throw_On_Invalid_Options()
  {
    var provider = CreateProvider();

    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        provider.GetOrSetAsync("key", _ => Task.FromResult("v"), new CacheOptions { Expiration = TimeSpan.Zero })
    );

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.GetOrSetAsync("key", _ => Task.FromResult("v"), new CacheOptions { LocalCacheHint = TimeSpan.FromSeconds(1) })
    );

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.GetOrSetAsync("key", _ => Task.FromResult("v"), new CacheOptions { Tags = new[] { "tag1" } })
    );
  }

  [Fact]
  public async Task RemoveByTagAsync_Should_Throw_NotSupported()
  {
    var provider = CreateProvider();

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.RemoveByTagAsync("tag1")
    );
  }
}
