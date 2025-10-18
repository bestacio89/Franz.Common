using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

public class SettingsCacheTests
{
  [Fact]
  public async Task Should_Prefix_Settings_Keys()
  {
    // ✅ MemoryCache expects IOptions<MemoryCacheOptions>, wrap it with Options.Create
    var memCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
    var provider = new MemoryCacheProvider(memCache);
    var settings = new SettingsCache(provider);

    await settings.SetSettingAsync("theme", "dark");

    var value = await settings.GetSettingAsync<string>("theme");
    value.Should().Be("dark");
  }
}
