using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Franz.Common.Caching.Providers;

public sealed class RedisCacheProvider : ICacheProvider
{
  private readonly IConnectionMultiplexer _multiplexer;
  private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;
  private readonly IDatabase _db;

  public RedisCacheProvider(
      IConnectionMultiplexer connection,
      IOptionsMonitor<CacheOptions> optionsMonitor)
  {
    _multiplexer = connection ?? throw new ArgumentNullException(nameof(connection));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    _db = _multiplexer.GetDatabase();
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    var settings = _optionsMonitor.CurrentValue;
    var fullKey = ApplyPrefix(key, settings.KeyPrefix);

    // 🔹 1. Try cache (Standard Get)
    var value = await _db.StringGetAsync(fullKey);
    if (value.HasValue)
    {
      var deserialized = JsonSerializer.Deserialize<T>((string)value!)!;
      return new CacheResult<T>(deserialized, IsHit: true);
    }

    // 🔹 2. Cache miss → compute
    var computed = await factory(ct);
    var serialized = JsonSerializer.Serialize(computed);
    var expiration = GetExpiration(requestOptions, settings);

    // 🔹 3. Atomic Write (Standard Set)
    await _db.StringSetAsync(fullKey, serialized, expiration);

    return new CacheResult<T>(computed, IsHit: false);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    var settings = _optionsMonitor.CurrentValue;
    await _db.KeyDeleteAsync(ApplyPrefix(key, settings.KeyPrefix));
  }

  /// <summary>
  /// Intentionally unsupported to maintain O(1) performance and lean Redis state.
  /// </summary>
  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
      => throw new NotSupportedException("Tag-based invalidation is disabled for performance reasons.");

  #region Helpers

  private static string ApplyPrefix(string key, string prefix) => $"{prefix}{key}";

  private static TimeSpan GetExpiration(CacheOptions? request, CacheOptions global)
  {
    var requestAbsolute = request?.DefaultAbsoluteExpiration;
    if (requestAbsolute is not null &&
        requestAbsolute != default &&
        requestAbsolute != global.DefaultAbsoluteExpiration)
    {
      return requestAbsolute.Value;
    }

    var requestSliding = request?.DefaultSlidingExpiration;
    if (requestSliding is not null &&
        requestSliding != default &&
        requestSliding != global.DefaultSlidingExpiration)
    {
      return requestSliding.Value;
    }

    return global.DefaultAbsoluteExpiration;
  }

  #endregion
}