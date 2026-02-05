using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Distributed;

public sealed class DistributedCacheProvider : ICacheProvider
{
  private readonly IDistributedCache _cache;
  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

  public DistributedCacheProvider(IDistributedCache cache)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
    var cached = await _cache.GetStringAsync(key, ct);
    if (cached is not null)
    {
      var deserialized = JsonSerializer.Deserialize<T>(cached)!;
      return new CacheResult<T>(deserialized, IsHit: true);
    }

    // 🔹 2. Cache miss → compute
    // ⚠ No stampede protection by design
    var value = await factory(ct);
    var json = JsonSerializer.Serialize(value);

    await _cache.SetStringAsync(
        key,
        json,
        CreateDistributedCacheOptions(options),
        ct);

    return new CacheResult<T>(value, IsHit: false);
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    return _cache.RemoveAsync(key, ct);
  }

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
    => throw new NotSupportedException(
        "Tag-based invalidation is not supported by DistributedCacheProvider.");

  // ============================
  // Helpers
  // ============================

  private static DistributedCacheEntryOptions CreateDistributedCacheOptions(
      CacheOptions? options)
  {
    var distributedOptions = new DistributedCacheEntryOptions();

    if (options is null)
    {
      distributedOptions.AbsoluteExpirationRelativeToNow = DefaultExpiration;
      return distributedOptions;
    }

    // Priority order:
    // AbsoluteExpirationRelativeToNow > AbsoluteExpiration > SlidingExpiration > Default
    if (options.AbsoluteExpirationRelativeToNow.HasValue)
    {
      distributedOptions.AbsoluteExpirationRelativeToNow =
          options.AbsoluteExpirationRelativeToNow.Value;
    }
    else if (options.AbsoluteExpiration.HasValue)
    {
      distributedOptions.AbsoluteExpiration =
          options.AbsoluteExpiration.Value;
    }
    else if (options.SlidingExpiration.HasValue)
    {
      distributedOptions.SlidingExpiration =
          options.SlidingExpiration.Value;
    }
    else
    {
      distributedOptions.AbsoluteExpirationRelativeToNow = DefaultExpiration;
    }

    return distributedOptions;
  }

  private static void ValidateOptions(CacheOptions? options)
  {
    if (options is null)
      return;

    if (options.AbsoluteExpirationRelativeToNow.HasValue &&
        options.AbsoluteExpirationRelativeToNow.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.AbsoluteExpirationRelativeToNow));

    if (options.SlidingExpiration.HasValue &&
        options.SlidingExpiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.SlidingExpiration));

    if (options.AbsoluteExpiration.HasValue &&
        options.AbsoluteExpiration.Value <= DateTimeOffset.UtcNow)
      throw new ArgumentOutOfRangeException(
          nameof(options.AbsoluteExpiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException(
          "LocalCacheHint is not applicable to distributed-only cache providers.");

    if (options.Tags is not null)
      throw new NotSupportedException(
          "Tags are not supported by DistributedCacheProvider.");
  }
}
