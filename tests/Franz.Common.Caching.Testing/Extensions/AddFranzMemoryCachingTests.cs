using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Estrategies;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Settings;
using Franz.Common.Caching.Testing.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Extensions;

public sealed class AddFranzMemoryCachingTests
{
  [Fact]
  public void Should_Register_MemoryCache_Infrastructure()
  {
    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzMemoryCaching());

    sp.GetRequiredService<ICacheProvider>()
      .Should().BeOfType<MemoryCacheProvider>();

    sp.GetRequiredService<ICacheKeyStrategy>()
      .Should().BeOfType<DefaultCacheKeyStrategy>();

    sp.GetRequiredService<ISettingsCache>()
      .Should().BeOfType<SettingsCache>();
  }

  [Fact]
  public void Should_Apply_CacheEntryOptions()
  {
    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzMemoryCaching(o => o.Ttl = TimeSpan.FromSeconds(10)));

    var options = sp.GetRequiredService<
      Microsoft.Extensions.Options.IOptions<CacheEntryOptions>>();

    options.Value.Ttl.Should().Be(TimeSpan.FromSeconds(10));
  }
}

