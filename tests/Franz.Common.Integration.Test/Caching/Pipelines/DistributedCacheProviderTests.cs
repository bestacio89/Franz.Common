using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

public class DistributedCacheProviderTests
{
  private static IDistributedCache CreateMemoryDistributedCache() =>
      new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

  private sealed record User(string Name);

  [Fact]
  public async Task GetOrSetAsync_Should_Set_And_Get_Value()
  {
    var cache = CreateMemoryDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var key = "user:1";

    var value = await provider.GetOrSetAsync(key, _ => Task.FromResult(new User("John")),
        new CacheOptions { Expiration = TimeSpan.FromMinutes(1) });

    value.Should().NotBeNull();
    ((User)value).Name.Should().Be("John");

    // Fetch again to hit cache
    var cached = await provider.GetOrSetAsync(key, _ => Task.FromResult(new User("Jane")));
    ((User)cached).Name.Should().Be("John"); // should return cached value
  }

  [Fact]
  public async Task RemoveAsync_Should_Delete_Entry()
  {
    var cache = CreateMemoryDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var key = "temp";
    await provider.GetOrSetAsync(key, _ => Task.FromResult(42), new CacheOptions { Expiration = TimeSpan.FromMinutes(1) });

    await provider.RemoveAsync(key);

    var result = await provider.GetOrSetAsync<int?>(key, _ => Task.FromResult<int?>(0));
    result.Should().Be(0); // factory value returned after removal
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Throw_On_Null_Key()
  {
    var cache = CreateMemoryDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        provider.GetOrSetAsync<int?>(null!, _ => Task.FromResult<int?>(1)));
  }
}
