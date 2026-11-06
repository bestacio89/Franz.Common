using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Internal;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.Hybrid;

/// <summary>
/// Adapts the .NET HybridCache API to Franz.Common's provider contract.
/// HybridCache supplies its own local stampede protection and L1/L2 coordination.
/// </summary>
public sealed class HybridCacheProvider : ICacheProvider, ICacheWriter
{
  private static readonly TimeSpan NoExpiration = TimeSpan.FromDays(36500);
  private readonly HybridCache _cache;
  private readonly IOptionsMonitor<CacheOptions>? _optionsMonitor;
  private readonly CacheOptions? _fixedOptions;

  public HybridCacheProvider(HybridCache cache, CacheOptions options)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _fixedOptions = options ?? throw new ArgumentNullException(nameof(options));
  }

  public HybridCacheProvider(HybridCache cache, IOptionsMonitor<CacheOptions> optionsMonitor)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? overrideOptions = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(factory);
    ct.ThrowIfCancellationRequested();

    var options = overrideOptions ?? CurrentOptions;
    CacheOptionsValidator.ThrowIfInvalid(options);
    var effectiveKey = ApplyPrefix(key);
    var isHit = true;

    var value = await _cache.GetOrCreateAsync<Func<CancellationToken, Task<T>>, T>(
        effectiveKey,
        factory,
        async (f, cancellationToken) =>
        {
          isHit = false;
          var created = await f(cancellationToken).ConfigureAwait(false);
          cancellationToken.ThrowIfCancellationRequested();
          return created;
        },
        CreateEntryOptions(options),
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
    ct.ThrowIfCancellationRequested();

    var effectiveOptions = options ?? CurrentOptions;
    CacheOptionsValidator.ThrowIfInvalid(effectiveOptions);
    await _cache.SetAsync(
        ApplyPrefix(key),
        value,
        CreateEntryOptions(effectiveOptions),
        cancellationToken: ct).ConfigureAwait(false);
  }

  public Task RemoveAsync(string key, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    return _cache.RemoveAsync(ApplyPrefix(key), ct).AsTask();
  }

  private CacheOptions CurrentOptions => _optionsMonitor?.CurrentValue ?? _fixedOptions!;

  private string ApplyPrefix(string key) => $"{CurrentOptions.KeyPrefix}{key}";

  private static HybridCacheEntryOptions CreateEntryOptions(CacheOptions options)
  {
    var absoluteExpiration = options.DefaultAbsoluteExpiration > TimeSpan.Zero
        ? options.DefaultAbsoluteExpiration
        : NoExpiration;
    var localExpiration = options.LocalCacheHint ??
        (options.DefaultSlidingExpiration > TimeSpan.Zero
            ? options.DefaultSlidingExpiration
            : NoExpiration);

    // L1 must never retain an entry beyond the configured absolute lifetime.
    if (localExpiration > absoluteExpiration)
    {
      localExpiration = absoluteExpiration;
    }

    return new HybridCacheEntryOptions
    {
      Expiration = absoluteExpiration,
      LocalCacheExpiration = localExpiration
    };
  }
}
