using Franz.Common.Caching.Abstractions;
using StackExchange.Redis;
using System.Text.Json;
namespace Franz.Common.Caching.Providers;

public sealed class RedisCacheProvider : ICacheProvider
{
  private readonly IDatabase _db;
  private readonly IConnectionMultiplexer _multiplexer;
  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

  public RedisCacheProvider(IConnectionMultiplexer connection)
  {
    _multiplexer = connection ?? throw new ArgumentNullException(nameof(connection));
    _db = _multiplexer.GetDatabase();
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
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

    // 🔹 1. Try cache
    var value = await _db.StringGetAsync(key);
    if (value.HasValue)
    {
      var deserialized = JsonSerializer.Deserialize<T>((string)value!)!;
      return new CacheResult<T>(deserialized, IsHit: true);
    }

    // 🔹 2. Cache miss → compute
    var computed = await factory(ct);
    var serialized = JsonSerializer.Serialize(computed);
    var expiration = GetExpiration(options);

    await _db.StringSetAsync(key, serialized, expiration);

    // 🔹 3. Handle tags
    if (options?.Tags != null)
    {
      foreach (var tag in options.Tags)
      {
        await _db.SetAddAsync($"tag:{tag}", key);
      }
    }

    return new CacheResult<T>(computed, IsHit: false);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    await _db.KeyDeleteAsync(key);

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
      await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)(string)k!).ToArray());
    }

    await _db.KeyDeleteAsync(tagSetKey);
  }

  private void ValidateOptions(CacheOptions? options)
  {
    if (options is null) return;

    if (options.AbsoluteExpirationRelativeToNow.HasValue &&
        options.AbsoluteExpirationRelativeToNow.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.AbsoluteExpirationRelativeToNow));

    if (options.SlidingExpiration.HasValue &&
        options.SlidingExpiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.SlidingExpiration));

    if (options.AbsoluteExpiration.HasValue &&
        options.AbsoluteExpiration.Value <= DateTimeOffset.UtcNow)
      throw new ArgumentOutOfRangeException(nameof(options.AbsoluteExpiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException("LocalCacheHint is not applicable to RedisCacheProvider.");
  }

  private TimeSpan GetExpiration(CacheOptions? options)
  {
    if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
      return options.AbsoluteExpirationRelativeToNow.Value;

    if (options?.AbsoluteExpiration.HasValue == true)
      return options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;

    if (options?.SlidingExpiration.HasValue == true)
      return options.SlidingExpiration.Value;

    return DefaultExpiration;
  }

  private IServer GetServer()
  {
    var endpoints = _multiplexer.GetEndPoints();
    return _multiplexer.GetServer(endpoints.First());
  }
}
