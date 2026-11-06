using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace Franz.Common.Caching.Tests.Abstractions;

public sealed class SettingsCacheTests
{
  [Fact]
  public void Interface_Should_Expose_GetSettingAsync()
  {
    typeof(ISettingsCache).GetMethod("GetSettingAsync")
      .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_SetSettingAsync()
  {
    typeof(ISettingsCache).GetMethod("SetSettingAsync")
      .Should().NotBeNull();
  }

  [Fact]
  public async Task SetSettingAsync_Should_ReplacePreviouslyCachedDefaultValue()
  {
    using var memory = new MemoryCache(new MemoryCacheOptions());
    var options = new Mock<IOptionsMonitor<CacheOptions>>();
    options.SetupGet(x => x.CurrentValue).Returns(new CacheOptions());
    var provider = new MemoryCacheProvider(memory, options.Object);
    var settings = new SettingsCache(provider);

    var missing = await settings.GetSettingAsync<string>("theme");
    missing.Should().BeNull();

    await settings.SetSettingAsync("theme", "dark");

    var actual = await settings.GetSettingAsync<string>("theme");
    actual.Should().Be("dark");
  }
}
