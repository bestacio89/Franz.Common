using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Providers;
public class MemoryCacheProvider : ICacheProvider
{
  private readonly IMemoryCache _cache;
  public MemoryCacheProvider(IMemoryCache cache) => _cache = cache;

  public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
      Task.FromResult(_cache.TryGetValue(key, out var value) ? (T?)value : default);

  public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
  {
    _cache.Set(key, value, ttl);
    return Task.CompletedTask;
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    _cache.Remove(key);
    return Task.CompletedTask;
  }

  public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
      Task.FromResult(_cache.TryGetValue(key, out _));
}
