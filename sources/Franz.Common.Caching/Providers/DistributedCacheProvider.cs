using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class DistributedCacheProvider : ICacheProvider
{
  private readonly IDistributedCache _cache;
  public DistributedCacheProvider(IDistributedCache cache) => _cache = cache;

  public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
  {
    var data = await _cache.GetStringAsync(key, ct);
    return data is null ? default : JsonSerializer.Deserialize<T>(data);
  }

  public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
  {
    var data = JsonSerializer.Serialize(value);
    await _cache.SetStringAsync(key, data,
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
  }

  public Task RemoveAsync(string key, CancellationToken ct = default) =>
      _cache.RemoveAsync(key, ct);

  public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
      await _cache.GetStringAsync(key, ct) is not null;
}
