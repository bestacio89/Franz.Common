using FluentAssertions;
using Franz.Common.Caching.Testing.Fixtures;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Providers;

public sealed class RedisCacheProviderTests
  : IClassFixture<RedisCacheFixture>
{
  private readonly RedisCacheFixture _fixture;

  public RedisCacheProviderTests(RedisCacheFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task Set_And_Get_Should_Work()
  {
    var muxer = ConnectionMultiplexer.Connect(_fixture.ConnectionString);
    var provider = new RedisCacheProvider(muxer);

    await provider.SetAsync("key", 42, TimeSpan.FromSeconds(30));
    var value = await provider.GetAsync<int>("key");

    value.Should().Be(42);
  }
}

