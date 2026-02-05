using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Observability;
using StackExchange.Redis;
using System.Text.Json;

namespace Franz.Common.Caching.Redis;

public sealed class RedisCacheProvider : ICacheProvider
{
  private readonly IDatabase _db;
  private readonly IConnectionMultiplexer _multiplexer;
  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);
  private readonly ICacheObserver[] _observers;

  public RedisCacheProvider(IConnectionMultiplexer connection, IEnumerable<ICacheObserver>? observers = null)
  {
    _multiplexer = connection ?? throw new ArgumentNullException(nameof(connection));
    _db = _multiplexer.GetDatabase();
    _observers = observers?.ToArray() ?? Array.Empty<ICacheObserver>();
  }

  public async Task<T?> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));

    ValidateOptions(options);

    // Try get from Redis
    var value = await _db.StringGetAsync(key);
    if (value.HasValue)
    {
      NotifyHit(key);
      return JsonSerializer.Deserialize<T>((string)value!, new JsonSerializerOptions());
    }

    // Cache miss -> compute
    var computed = await factory(ct);
    var serialized = JsonSerializer.Serialize(computed);
    await _db.StringSetAsync(key, serialized, options?.Expiration ?? DefaultExpiration);

    NotifySet(key, computed, options?.Expiration ?? DefaultExpiration);

    // Handle tags
    if (options?.Tags != null)
    {
      foreach (var tag in options.Tags)
      {
        var tagSetKey = $"tag:{tag}";
        await _db.SetAddAsync(tagSetKey, key);
      }
    }

    return computed;
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    await _db.KeyDeleteAsync(key);
    NotifyRemove(key);

    // Remove from all tag sets
    var server = GetServer();
    foreach (var tagKey in server.Keys(pattern: "tag:*"))
    {
      await _db.SetRemoveAsync(tagKey, key);
    }
  }

  public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
  {
    var tagSetKey = $"tag:{tag}";
    var keys = await _db.SetMembersAsync(tagSetKey);

    if (keys.Length > 0)
    {
      await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)(string)k).ToArray());
      foreach (var k in keys)
        NotifyRemove(k);
    }

    await _db.KeyDeleteAsync(tagSetKey);
    NotifyRemoveByTag(tag);
  }

  private void ValidateOptions(CacheOptions? options)
  {
    if (options is null) return;

    if (options.Expiration.HasValue && options.Expiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.Expiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException("LocalCacheHint is not applicable to RedisCacheProvider.");
  }

  private IServer GetServer()
  {
    var endpoints = _multiplexer.GetEndPoints();
    return _multiplexer.GetServer(endpoints.First());
  }

  #region Observer notifications

  private void NotifySet<T>(string key, T value, TimeSpan expiration)
  {
    var entry = new CacheEntryDescriptor
    {
      Key = key,
      EstimatedSizeInBytes = value?.ToString()?.Length ?? 0,
      Ttl = expiration
    };

    foreach (var obs in _observers)
      obs.OnCacheSet(entry);
  }

  private void NotifyHit(string key)
  {
    var access = new CacheAccessDescriptor
    {
      Key = key,
      AccessedAt = DateTime.UtcNow
    };

    foreach (var obs in _observers)
      obs.OnCacheHit(access);
  }

  private void NotifyRemove(string key)
  {
    foreach (var obs in _observers)
      obs.OnCacheRemove(key);
  }

  private void NotifyRemoveByTag(string tag)
  {
    foreach (var obs in _observers)
      obs.OnCacheRemoveByTag(tag);
  }

  #endregion
}
