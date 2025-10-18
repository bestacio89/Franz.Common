using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class DistributedCacheProviderTests
{
  private static IDistributedCache CreateMemoryDistributedCache() =>
      new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

  [Fact]
  public async Task Should_Set_And_Get_Value()
  {
    var cache = CreateMemoryDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await provider.SetAsync("user:1", new { Name = "John" }, TimeSpan.FromMinutes(1));
    var result = await provider.GetAsync<JsonElement>("user:1");

    result.GetProperty("Name").GetString().Should().Be("John");
  }

  [Fact]
  public async Task ExistsAsync_Should_Return_True_When_Value_Present()
  {
    var cache = CreateMemoryDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await provider.SetAsync("exists", "ok", TimeSpan.FromMinutes(1));
    var exists = await provider.ExistsAsync("exists");

    exists.Should().BeTrue();
  }

  [Fact]
  public async Task RemoveAsync_Should_Delete_Entry()
  {
    var cache = CreateMemoryDistributedCache();
    var provider = new DistributedCacheProvider(cache);

    await provider.SetAsync("temp", 42, TimeSpan.FromMinutes(1));
    await provider.RemoveAsync("temp");

    var result = await provider.GetAsync<int?>("temp");
    result.Should().BeNull();
  }
}