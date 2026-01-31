using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Testing.Fakes;
using Xunit;

namespace Franz.Common.Caching.Testing.Providers;

public sealed class DistributedCacheProviderTests
{
  private sealed record TestPayload(int Id, string Name);

  [Fact]
  public async Task GetOrSet_Should_Store_And_Return_Value()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var result = await provider.GetOrSetAsync(
        "dist:test:basic",
        _ => Task.FromResult(new TestPayload(1, "franz")));

    result.Should().Be(new TestPayload(1, "franz"));
  }

  [Fact]
  public async Task GetOrSet_Should_Return_Cached_Value_On_Second_Call()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var calls = 0;

    Func<CancellationToken, Task<TestPayload>> factory = _ =>
    {
      calls++;
      return Task.FromResult(new TestPayload(2, "cached"));
    };

    var key = "dist:test:cached";

    var first = await provider.GetOrSetAsync(key, factory);
    var second = await provider.GetOrSetAsync(key, factory);

    first.Should().Be(second);
    calls.Should().Be(1);
  }

  [Fact]
  public async Task Should_Respect_Expiration()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var key = "dist:test:ttl";

    await provider.GetOrSetAsync(
        key,
        _ => Task.FromResult("value"),
        new CacheOptions
        {
          Expiration = TimeSpan.FromMilliseconds(200)
        });

    await Task.Delay(300);

    var result = await provider.GetOrSetAsync(
        key,
        _ => Task.FromResult("new"));

    result.Should().Be("new");
  }

  [Fact]
  public async Task Remove_Should_Delete_Key()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var key = "dist:test:remove";

    await provider.GetOrSetAsync(key, _ => Task.FromResult(123));
    await provider.RemoveAsync(key);

    var result = await provider.GetOrSetAsync(
        key,
        _ => Task.FromResult(456));

    result.Should().Be(456);
  }

  [Fact]
  public async Task Should_Throw_When_Key_Is_Invalid()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        provider.GetOrSetAsync<int>("", _ => Task.FromResult(1)));
  }

  [Fact]
  public async Task Should_Throw_When_Factory_Is_Null()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        provider.GetOrSetAsync<int>("key", null!));
  }

  [Fact]
  public async Task Should_Reject_LocalCacheHint()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.GetOrSetAsync(
            "dist:test:local",
            _ => Task.FromResult(1),
            new CacheOptions
            {
              LocalCacheHint = TimeSpan.FromSeconds(1)
            }));
  }

  [Fact]
  public async Task Should_Reject_Tags()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.GetOrSetAsync(
            "dist:test:tags",
            _ => Task.FromResult(1),
            new CacheOptions
            {
              Tags = new[] { "orders" }
            }));
  }

  [Fact]
  public async Task RemoveByTag_Should_Throw()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.RemoveByTagAsync("any"));
  }
}
