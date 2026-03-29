using FluentAssertions;
using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Distributed;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Providers; // Corrected namespace for the Provider
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Tests.Extensions;

public sealed class AddFranzDistributedCachingTests
{
  private sealed class FakeDistributedCache : IDistributedCache
  {
    private readonly Dictionary<string, byte[]> _store = new();
    public byte[]? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken _ = default) => Task.FromResult(Get(key));
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken _ = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public Task RemoveAsync(string key, CancellationToken _ = default) { Remove(key); return Task.CompletedTask; }
    public void Set(string key, byte[] value, DistributedCacheEntryOptions _) => _store[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions opts, CancellationToken tok = default)
    { Set(key, value, opts); return Task.CompletedTask; }
  }

  [Fact]
  public void Should_Register_DistributedCache_Provider_With_Reactive_Options()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act 
    // 1. We register the underlying IDistributedCache (the "Engine")
    services.AddSingleton<IDistributedCache, FakeDistributedCache>();

    // 2. We use the new extension that bridges IDistributedCache to our ICacheProvider
    // This must also initialize the required CacheOptions
    services.AddFranzDistributedCaching (options =>
    {
      options.KeyPrefix = "dist:";
      options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(10);
    });

    var sp = services.BuildServiceProvider();

    // Assert
    var provider = sp.GetRequiredService<ICacheProvider>();
    provider.Should().BeOfType<DistributedCacheProvider>();

    // Verify the provider is actually using the reactive options
    var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CacheOptions>>();
    optionsMonitor.CurrentValue.KeyPrefix.Should().Be("dist:");
  }
}