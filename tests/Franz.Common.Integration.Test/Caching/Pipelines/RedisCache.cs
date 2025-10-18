using Franz.Common.Caching.Abstractions;
using StackExchange.Redis;
using Xunit;
using FluentAssertions;

public class RedisCacheProviderTests
{
  private static readonly string RedisConn = "localhost:6379";

  [Fact(Skip = "Requires local Redis instance (localhost:6379)")]
  public async Task Should_Set_And_Get_Value_From_Redis()
  {
    var muxer = await ConnectionMultiplexer.ConnectAsync(RedisConn);
    var provider = new RedisCacheProvider(muxer);

    await provider.SetAsync("key", "value", TimeSpan.FromMinutes(1));

    var result = await provider.GetAsync<string>("key");
    result.Should().Be("value");
  }

  [Fact(Skip = "Requires local Redis instance (localhost:6379)")]
  public async Task ExistsAsync_Should_Return_True_When_Key_Exists()
  {
    var muxer = await ConnectionMultiplexer.ConnectAsync(RedisConn);
    var provider = new RedisCacheProvider(muxer);

    await provider.SetAsync("exists", "yes", TimeSpan.FromMinutes(1));

    var exists = await provider.ExistsAsync("exists");
    exists.Should().BeTrue();
  }
}
