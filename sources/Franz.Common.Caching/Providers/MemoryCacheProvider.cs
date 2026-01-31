using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Franz.Common.Caching.Providers;

public sealed class MemoryCacheProvider : ICacheProvider
{
  private readonly IMemoryCache _cache;

  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

  public MemoryCacheProvider(IMemoryCache cache)
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

    if (_cache.TryGetValue(key, out var existing))
      return (T?)existing;

    // ⚠ No stampede protection by design
    var value = await factory(ct);

    _cache.Set(
        key,
        value!,
        new MemoryCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow =
                options?.Expiration ?? DefaultExpiration
        });

    return value;
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    _cache.Remove(key);
    return Task.CompletedTask;
  }

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
      => throw new NotSupportedException(
          "Tag-based invalidation is not supported by MemoryCacheProvider.");

  private static void ValidateOptions(CacheOptions? options)
  {
    if (options is null)
      return;

    if (options.Expiration != null && options.Expiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options.Expiration));

    if (options.LocalCacheHint is not null)
      throw new NotSupportedException(
          "LocalCacheHint is not applicable to memory-only cache providers.");

    if (options.Tags is not null)
      throw new NotSupportedException(
          "Tags are not supported by MemoryCacheProvider.");
  }
}
