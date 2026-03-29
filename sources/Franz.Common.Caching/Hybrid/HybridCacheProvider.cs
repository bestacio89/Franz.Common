using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Hybrid;

/// <summary>
/// A high-performance HybridCache implementation for Franz.Common.
/// Optimized for .NET 10 and aligned with the simplified CacheOptions schema.
/// </summary>
public sealed class HybridCacheProvider : ICacheProvider
{
  private readonly HybridCache _cache;
  private readonly CacheOptions _defaultOptions;

  public HybridCacheProvider(HybridCache cache, CacheOptions options)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _defaultOptions = options ?? throw new ArgumentNullException(nameof(options));
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? overrideOptions = null,
      CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));

    // Apply KeyPrefix from the "Correct Cereal" options
    var effectiveKey = $"{_defaultOptions.KeyPrefix}{key}";
    var isHit = true;

    var value = await _cache.GetOrCreateAsync<Func<CancellationToken, Task<T>>, T>(
        effectiveKey,
        factory,
        async (f, cancellationToken) =>
        {
          isHit = false; // Factory invoked → Cache MISS
          return await f(cancellationToken);
        },
        CreateEntryOptions(overrideOptions ?? _defaultOptions),
        cancellationToken: ct
    );

    return new CacheResult<T>(value, isHit);
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

    var effectiveKey = $"{_defaultOptions.KeyPrefix}{key}";

    // Use native ValueTask conversion for hot-path efficiency
    return _cache.RemoveAsync(effectiveKey, ct).AsTask();
  }

  /// <summary>
  /// Note: RemoveByTagAsync is deprecated in this provider to maintain 
  /// high-performance deterministic invalidation.
  /// </summary>
  [Obsolete("Tag-based invalidation is disabled for performance consistency. Use key-based removal.")]
  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
  {
    throw new NotSupportedException("Tag-based invalidation is highly inefficient in distributed contexts and has been removed from Franz.Common.Caching.");
  }

  private static HybridCacheEntryOptions CreateEntryOptions(CacheOptions options)
      => new()
      {
        // Mapping Absolute Expiration to HybridCache
        Expiration = options.DefaultAbsoluteExpiration,

        // LocalCacheHint allows for L1 (Memory) vs L2 (Distributed) tiering
        LocalCacheExpiration = options.LocalCacheHint ?? options.DefaultSlidingExpiration
      };
}