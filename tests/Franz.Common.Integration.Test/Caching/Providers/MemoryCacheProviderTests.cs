using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Microsoft.Extensions.Caching.Memory;
using FluentAssertions;
using Xunit;

public class MemoryCacheProviderTests
{
  [Fact]
  public async Task GetOrSetAsync_Should_Return_Cached_Value()
  {
    var provider = new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));

    // First call: factory executes
    var first = await provider.GetOrSetAsync("user:1", _ => Task.FromResult("Alice"));
    first.Should().Be("Alice");

    // Second call: returns cached value
    var second = await provider.GetOrSetAsync("user:1", _ => Task.FromResult("Bob"));
    second.Should().Be("Alice"); // still "Alice" because cached
  }

  [Fact]
  public async Task RemoveAsync_Should_Delete_Entry()
  {
    var provider = new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));

    await provider.GetOrSetAsync("temp", _ => Task.FromResult(42));
    await provider.RemoveAsync("temp");

    // Now factory executes again because cache was removed
    var result = await provider.GetOrSetAsync("temp", _ => Task.FromResult(99));
    result.Should().Be(99);
  }

  [Fact]
  public async Task GetOrSetAsync_Should_Throw_On_Invalid_Key()
  {
    var provider = new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));

    await FluentActions
        .Invoking(() => provider.GetOrSetAsync<string>("", _ => Task.FromResult("x")))
        .Should().ThrowAsync<ArgumentException>();
  }
}
