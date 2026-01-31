using Franz.Common.Caching.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Settings;

public sealed class SettingsCache : ISettingsCache
{
  private readonly ICacheProvider _cache;
  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(12);

  public SettingsCache(ICacheProvider cache)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
  }

  /// <summary>
  /// Gets a setting from the cache. Returns null if the key doesn't exist.
  /// </summary>
  public Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty.", nameof(key));

    // Use GetOrSetAsync with a factory that returns default(T) for missing keys
    return _cache.GetOrSetAsync(
        $"settings:{key}",
        _ => Task.FromResult(default(T)!),
        new CacheOptions { Expiration = DefaultExpiration },
        ct
    );
  }

  /// <summary>
  /// Sets a setting in the cache with a default expiration.
  /// </summary>
  public Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty.", nameof(key));

    // Use GetOrSetAsync to store the value
    return _cache.GetOrSetAsync(
        $"settings:{key}",
        _ => Task.FromResult(value!),
        new CacheOptions { Expiration = DefaultExpiration },
        ct
    );
  }
}
