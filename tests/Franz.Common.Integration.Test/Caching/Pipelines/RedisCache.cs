using Franz.Common.Caching.Abstractions;
using StackExchange.Redis;
using Xunit;
using FluentAssertions;
using Franz.Common.Caching.Redis;
using System;
using System.Threading.Tasks;

public class RedisCacheProviderTests
{
  private static readonly string RedisConn = "localhost:6379";

  private sealed record User(string Name);

  [Fact(Skip = "Requires local Redis instance (localhost:6379)")]
  public async Task GetOrSetAsync_Should_Set_And_Get_Value()
  {
    var muxer = await ConnectionMultiplexer.ConnectAsync(RedisConn);
    var provider = new RedisCacheProvider(muxer);

    var key = "user:1";

    // First call: MISS, factory executes
    var value = await provider.GetOrSetAsync(key, _ => Task.FromResult(new User("John")),
        new CacheOptions { Expiration = TimeSpan.FromMinutes(1) });

    value.Should().NotBeNull();
    ((User)value).Name.Should().Be("John");

    // Second call: HIT, factory ignored
    var cached = await provider.GetOrSetAsync(key, _ => Task.FromResult(new User("Jane")));
    ((User)cached).Name.Should().Be("John"); // still cached
  }

  [Fact(Skip = "Requires local Redis instance (localhost:6379)")]
  public async Task RemoveAsync_Should_Delete_Key()
  {
    var muxer = await ConnectionMultiplexer.ConnectAsync(RedisConn);
    var provider = new RedisCacheProvider(muxer);

    var key = "temp";
    await provider.GetOrSetAsync(key, _ => Task.FromResult(42), new CacheOptions { Expiration = TimeSpan.FromMinutes(1) });

    await provider.RemoveAsync(key);

    var result = await provider.GetOrSetAsync<int?>(key, _ => Task.FromResult<int?>(0));
    result.Should().Be(0); // factory runs again
  }

  [Fact(Skip = "Requires local Redis instance (localhost:6379)")]
  public async Task GetOrSetAsync_Should_Throw_On_Empty_Key()
  {
    var muxer = await ConnectionMultiplexer.ConnectAsync(RedisConn);
    var provider = new RedisCacheProvider(muxer);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        provider.GetOrSetAsync<int?>(string.Empty, _ => Task.FromResult<int?>(1)));
  }
}
