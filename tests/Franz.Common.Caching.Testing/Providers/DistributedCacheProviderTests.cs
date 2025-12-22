using FluentAssertions;
using Franz.Common.Caching.Testing.Fakes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Providers;

public sealed class DistributedCacheProviderTests
{
  private sealed record TestPayload(int Id, string Name);

  [Fact]
  public async Task Set_And_Get_Should_Roundtrip_Value()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    var payload = new TestPayload(1, "franz");

    await provider.SetAsync("key", payload, TimeSpan.FromMinutes(1));
    var result = await provider.GetAsync<TestPayload>("key");

    result.Should().Be(payload);
  }

  [Fact]
  public async Task Exists_Should_Return_True_When_Key_Exists()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await provider.SetAsync("key", 42, TimeSpan.FromMinutes(1));

    (await provider.ExistsAsync("key")).Should().BeTrue();
  }

  [Fact]
  public async Task Exists_Should_Return_False_When_Key_Does_Not_Exist()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    (await provider.ExistsAsync("missing")).Should().BeFalse();
  }

  [Fact]
  public async Task Remove_Should_Delete_Key()
  {
    var cache = new FakeDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await provider.SetAsync("key", "value", TimeSpan.FromMinutes(1));
    await provider.RemoveAsync("key");

    (await provider.ExistsAsync("key")).Should().BeFalse();
  }
}
