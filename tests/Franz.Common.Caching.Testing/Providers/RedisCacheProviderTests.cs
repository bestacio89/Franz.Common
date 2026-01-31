using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Redis;
using Franz.Common.Caching.Testing.Fixtures;
using StackExchange.Redis;
using Xunit;

namespace Franz.Common.Caching.Testing.Providers;

public sealed class RedisCacheProviderTests
    : IClassFixture<RedisCacheFixture>
{
  private readonly RedisCacheProvider _provider;

  public RedisCacheProviderTests(RedisCacheFixture fixture)
  {
    var muxer = ConnectionMultiplexer.Connect(fixture.ConnectionString);
    _provider = new RedisCacheProvider(muxer);
  }

  [Fact]
  public async Task GetOrSet_Should_Store_And_Return_Value()
  {
    var result = await _provider.GetOrSetAsync(
        "redis:test:basic",
        _ => Task.FromResult(42));

    result.Should().Be(42);
  }

  [Fact]
  public async Task GetOrSet_Should_Return_Cached_Value_On_Second_Call()
  {
    var calls = 0;

    Func<CancellationToken, Task<int>> factory = _ =>
    {
      calls++;
      return Task.FromResult(99);
    };

    var key = "redis:test:cached";

    var first = await _provider.GetOrSetAsync(key, factory);
    var second = await _provider.GetOrSetAsync(key, factory);

    first.Should().Be(99);
    second.Should().Be(99);
    calls.Should().Be(1);
  }

  [Fact]
  public async Task Should_Respect_Expiration()
  {
    var key = "redis:test:ttl";

    await _provider.GetOrSetAsync(
        key,
        _ => Task.FromResult("value"),
        new CacheOptions
        {
          Expiration = TimeSpan.FromMilliseconds(200)
        });

    await Task.Delay(300);

    var result = await _provider.GetOrSetAsync(
        key,
        _ => Task.FromResult("new"));

    result.Should().Be("new");
  }

  [Fact]
  public async Task Remove_Should_Delete_Key()
  {
    var key = "redis:test:remove";

    await _provider.GetOrSetAsync(key, _ => Task.FromResult(123));
    await _provider.RemoveAsync(key);

    var value = await _provider.GetOrSetAsync(
        key,
        _ => Task.FromResult(456));

    value.Should().Be(456);
  }

  [Fact]
  public async Task Should_Throw_When_Key_Is_Invalid()
  {
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _provider.GetOrSetAsync<int>("", _ => Task.FromResult(1)));
  }

  [Fact]
  public async Task Should_Throw_When_Factory_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        _provider.GetOrSetAsync<int>("key", null!));
  }

  [Fact]
  public async Task Should_Reject_LocalCacheHint()
  {
    await Assert.ThrowsAsync<NotSupportedException>(() =>
        _provider.GetOrSetAsync(
            "redis:test:local",
            _ => Task.FromResult(1),
            new CacheOptions
            {
              LocalCacheHint = TimeSpan.FromSeconds(1)
            }));
  }

  [Fact]
  public async Task Should_Reject_Tags()
  {
    await Assert.ThrowsAsync<NotSupportedException>(() =>
        _provider.GetOrSetAsync(
            "redis:test:tags",
            _ => Task.FromResult(1),
            new CacheOptions
            {
              Tags = new[] { "orders" }
            }));
  }

  [Fact]
  public async Task RemoveByTag_Should_Throw()
  {
    await Assert.ThrowsAsync<NotSupportedException>(() =>
        _provider.RemoveByTagAsync("any"));
  }
}
