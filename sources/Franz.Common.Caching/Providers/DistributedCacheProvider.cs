using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Franz.Common.Caching.Distributed;

public sealed class DistributedCacheProvider : ICacheProvider
{
  private readonly IDistributedCache _cache;

  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

  public DistributedCacheProvider(IDistributedCache cache)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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

    var cached = await _cache.GetStringAsync(key, ct);
    if (cached is not null)
      return JsonSerializer.Deserialize<T>(cached);

    // ⚠ No stampede protection here by design
    // This provider is NOT hybrid, NOT concurrent-safe
    var value = await factory(ct);

    var json = JsonSerializer.Serialize(value);
    await _cache.SetStringAsync(
        key,
        json,
        new DistributedCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow =
                options?.Expiration ?? DefaultExpiration
        },
        ct);

    return value;
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

  private static void ValidateOptions(CacheOptions? options)
  {
    if (options is null)
      return;


    if (options.Expiration != null && options.Expiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.Expiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException(
          "LocalCacheHint is not applicable to distributed-only cache providers.");

    if (options.Tags is not null)
      throw new NotSupportedException(
          "Tags are not supported by DistributedCacheProvider.");
  }
}
