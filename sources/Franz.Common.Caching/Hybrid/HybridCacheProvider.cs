using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Franz.Common.Caching.Hybrid;

public sealed class HybridCacheProvider : ICacheProvider
{
  private readonly HybridCache _cache;

  // Franz-level defaults (boring, predictable)
  private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);
  private static readonly TimeSpan DefaultLocalHint = TimeSpan.FromMinutes(5);

  public HybridCacheProvider(HybridCache cache)
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

    // Explicitly specify type arguments and adapt factory to ValueTask<T>
    return await _cache.GetOrCreateAsync<Func<CancellationToken, Task<T>>, T>(
        key,
        factory,
        static async (f, cancellationToken) => await f(cancellationToken),
        CreateEntryOptions(options),
        tags: options?.Tags,
        cancellationToken: ct
    );
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    return _cache.RemoveAsync(key, ct).AsTask();
  }

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(tag))
      throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));

    return _cache.RemoveByTagAsync(tag, ct).AsTask();
  }

  private static HybridCacheEntryOptions CreateEntryOptions(CacheOptions? options)
      => new()
      {
        Expiration = options?.Expiration ?? DefaultExpiration,
        LocalCacheExpiration = options?.LocalCacheHint ?? DefaultLocalHint
      };

  private static void ValidateOptions(CacheOptions? options)
  {
    if (options is null)
      return;

    if (options.Expiration.HasValue && options.Expiration.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.Expiration),
          "Expiration must be greater than zero.");

    if (options.LocalCacheHint.HasValue && options.LocalCacheHint.Value <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.LocalCacheHint),
          "LocalCacheHint must be greater than zero.");
  }
}
