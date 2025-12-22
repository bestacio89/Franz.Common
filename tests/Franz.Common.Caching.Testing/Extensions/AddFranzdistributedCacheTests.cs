using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Testing.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Extensions;

public sealed class AddFranzDistributedCachingTests
{
  private sealed class FakeDistributedCache : IDistributedCache
  {
    private readonly Dictionary<string, byte[]> _store = new();
    public byte[]? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken _) => Task.FromResult(Get(key));
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken _) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public Task RemoveAsync(string key, CancellationToken _) { Remove(key); return Task.CompletedTask; }
    public void Set(string key, byte[] value, DistributedCacheEntryOptions _) => _store[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions opts, CancellationToken tok)
    { Set(key, value, opts); return Task.CompletedTask; }
  }

  [Fact]
  public void Should_Register_DistributedCache_Provider()
  {
    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzDistributedCaching<FakeDistributedCache>());

    sp.GetRequiredService<ICacheProvider>()
      .Should().BeOfType<DistributedCacheProvider>();
  }
}
