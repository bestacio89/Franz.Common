using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.Providers;

public sealed class MemoryCacheProvider : ICacheProvider
{
  private readonly IMemoryCache _cache;
  private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;
  private readonly AsyncKeyedLock<string> _locks = new();

  public MemoryCacheProvider(
      IMemoryCache cache,
      IOptionsMonitor<CacheOptions> optionsMonitor)
  {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(factory);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);

    if (_cache.TryGetValue(fullKey, out T? cached))
    {
      return new CacheResult<T>(cached!, IsHit: true);
    }

    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      if (_cache.TryGetValue(fullKey, out cached))
      {
        return new CacheResult<T>(cached!, IsHit: true);
      }

      var value = await factory(ct).ConfigureAwait(false);
      SetCore(fullKey, value, requestOptions);
      return new CacheResult<T>(value, IsHit: false);
    }
  }

  public async Task SetAsync<T>(
      string key,
      T value,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      SetCore(fullKey, value, requestOptions);
    }
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      _cache.Remove(fullKey);
    }
  }

  private string ApplyPrefix(string key) => $"{_optionsMonitor.CurrentValue.KeyPrefix}{key}";

  private void SetCore<T>(string fullKey, T value, CacheOptions? requestOptions)
  {
    var options = requestOptions ?? _optionsMonitor.CurrentValue;
    var entryOptions = new MemoryCacheEntryOptions
    {
      Priority = options.DefaultPriority
    };

    if (options.DefaultAbsoluteExpiration > TimeSpan.Zero)
    {
      entryOptions.AbsoluteExpirationRelativeToNow = options.DefaultAbsoluteExpiration;
    }

    if (options.DefaultSlidingExpiration > TimeSpan.Zero)
    {
      entryOptions.SlidingExpiration = options.DefaultSlidingExpiration;
    }

    if (options.DefaultEstimatedSizeInBytes > 0)
    {
      entryOptions.Size = options.DefaultEstimatedSizeInBytes;
    }

    _cache.Set(fullKey, value, entryOptions);
  }
}
