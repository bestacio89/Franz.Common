using Franz.Common.Caching.Providers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using FluentAssertions;

public class MemoryCacheProviderTests
{
  [Fact]
  public async Task Should_Set_And_Get_Value()
  {
    var provider = new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));

    await provider.SetAsync("key", "value", TimeSpan.FromMinutes(1));

    var result = await provider.GetAsync<string>("key");

    result.Should().Be("value");
  }

  [Fact]
  public async Task ExistsAsync_Should_Return_True_When_Key_Present()
  {
    var cache = new MemoryCache(new MemoryCacheOptions());
    var provider = new MemoryCacheProvider(cache);

    await provider.SetAsync("exists", 123, TimeSpan.FromMinutes(1));

    var exists = await provider.ExistsAsync("exists");
    exists.Should().BeTrue();
  }
}
