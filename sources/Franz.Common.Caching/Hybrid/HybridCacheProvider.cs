using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Franz.Common.Caching.Hybrid;

/// <summary>
/// Adapts the .NET HybridCache API to Franz.Common's provider contract.
/// HybridCache supplies its own local stampede protection and L1/L2 coordination.
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
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(factory);

    var effectiveKey = ApplyPrefix(key);
    var isHit = true;

    var value = await _cache.GetOrCreateAsync<Func<CancellationToken, Task<T>>, T>(
        effectiveKey,
        factory,
        async (f, cancellationToken) =>
        {
          isHit = false;
          return await f(cancellationToken).ConfigureAwait(false);
        },
        CreateEntryOptions(overrideOptions ?? _defaultOptions),
        cancellationToken: ct).ConfigureAwait(false);

    return new CacheResult<T>(value, isHit);
  }

  public async Task SetAsync<T>(
      string key,
      T value,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    await _cache.SetAsync(
        ApplyPrefix(key),
        value,
        CreateEntryOptions(options ?? _defaultOptions),
        cancellationToken: ct).ConfigureAwait(false);
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    return _cache.RemoveAsync(ApplyPrefix(key), ct).AsTask();
  }

  private string ApplyPrefix(string key) => $"{_defaultOptions.KeyPrefix}{key}";

  private static HybridCacheEntryOptions CreateEntryOptions(CacheOptions options)
      => new()
      {
        Expiration = options.DefaultAbsoluteExpiration > TimeSpan.Zero
            ? options.DefaultAbsoluteExpiration
            : null,
        LocalCacheExpiration = options.LocalCacheHint ??
            (options.DefaultSlidingExpiration > TimeSpan.Zero
                ? options.DefaultSlidingExpiration
                : null)
      };
}
