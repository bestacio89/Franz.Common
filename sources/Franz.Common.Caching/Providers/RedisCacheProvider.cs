using Franz.Common.Caching.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

public class RedisCacheProvider : ICacheProvider
{
  private readonly IDatabase _db;

  public RedisCacheProvider(IConnectionMultiplexer connection)
  {
    _db = connection.GetDatabase();
  }

  public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
  {
    var value = await _db.StringGetAsync(key);
    return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
  }

  public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
  {
    var serialized = JsonSerializer.Serialize(value);
    await _db.StringSetAsync(key, serialized, ttl);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    await _db.KeyDeleteAsync(key);
  }

  public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
  {
    return await _db.KeyExistsAsync(key);
  }
}
