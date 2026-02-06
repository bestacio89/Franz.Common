using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Testing.Fixtures;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    result.Value.Should().Be(42);
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

    var first = (await _provider.GetOrSetAsync(key, factory)).Value;
    var second = (await _provider.GetOrSetAsync(key, factory)).Value;

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

    var result = (await _provider.GetOrSetAsync(
        key,
        _ => Task.FromResult("new"))).Value;

    result.Should().Be("new");
  }

  [Fact]
  public async Task Remove_Should_Delete_Key()
  {
    var key = "redis:test:remove";

    await _provider.GetOrSetAsync(key, _ => Task.FromResult(123));
    await _provider.RemoveAsync(key);

    var value = (await _provider.GetOrSetAsync(
        key,
        _ => Task.FromResult(456))).Value;

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
  public async Task Should_Reject_Invalid_Expiration()
  {
    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        _provider.GetOrSetAsync(
            "key",
            _ => Task.FromResult(1),
            new CacheOptions { Expiration = TimeSpan.Zero }));
  }

  #region Tags

  [Fact]
  public async Task GetOrSet_With_Tags_Should_Store_Key_In_Tag_Set()
  {
    var key = "redis:test:tagged";
    var tag = "orders";

    await _provider.GetOrSetAsync(
        key,
        _ => Task.FromResult(10),
        new CacheOptions { Tags = new[] { tag } });

    // Confirm key exists in tag set in Redis
    var muxer = _provider.GetType()
                         .GetField("_multiplexer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                         .GetValue(_provider) as IConnectionMultiplexer;
    var db = muxer!.GetDatabase();
    var members = await db.SetMembersAsync($"tag:{tag}");
    members.Select(m => (string)m).Should().Contain(key);
  }

  [Fact]
  public async Task RemoveByTag_Should_Delete_All_Tagged_Keys()
  {
    var key1 = "redis:test:tag1";
    var key2 = "redis:test:tag2";
    var tag = "batch";

    await _provider.GetOrSetAsync(key1, _ => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
    await _provider.GetOrSetAsync(key2, _ => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });

    await _provider.RemoveByTagAsync(tag);

    // After removal, getting keys again should call factory
    var val1 = (await _provider.GetOrSetAsync(key1, _ => Task.FromResult(10))).Value;
    var val2 = (await _provider.GetOrSetAsync(key2, _ => Task.FromResult(20))).Value;

    val1.Should().Be(10);
    val2.Should().Be(20);

    // Tag set should be removed
    var muxer = _provider.GetType()
                         .GetField("_multiplexer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                         .GetValue(_provider) as IConnectionMultiplexer;
    var db = muxer!.GetDatabase();
    var tagMembers = await db.SetMembersAsync($"tag:{tag}");
    tagMembers.Should().BeEmpty();
  }

  #endregion
}
