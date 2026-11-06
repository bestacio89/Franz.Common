using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Settings;

public sealed class SettingsCache : ISettingsCache
{
  private readonly ICacheProvider _cache;
  private static readonly CacheOptions SettingsOptions = new()
  {
    DefaultAbsoluteExpiration = TimeSpan.FromHours(12),
    DefaultSlidingExpiration = TimeSpan.FromHours(12),
    DefaultEstimatedSizeInBytes = 0
  };

  public SettingsCache(ICacheProvider cache)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
  }

  /// <summary>
  /// Gets a setting from the cache. A missing setting resolves to default(T).
  /// </summary>
  public async Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var result = await _cache.GetOrSetAsync(
        $"settings:{key}",
        _ => Task.FromResult(default(T)!),
        SettingsOptions,
        ct).ConfigureAwait(false);

    return result.Value;
  }

  /// <summary>
  /// Unconditionally replaces the cached setting value.
  /// </summary>
  public Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    return _cache.SetAsync($"settings:{key}", value, SettingsOptions, ct);
  }
}
