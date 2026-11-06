#nullable enable
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Franz.Common.Caching.Distributed;

/// <summary>
/// Adapts IDistributedCache with process-local single-flight execution per key.
/// The lock prevents duplicate factories only within this provider instance; it
/// does not provide a cross-node distributed lock.
/// </summary>
public sealed class DistributedCacheProvider(
    IDistributedCache cache,
    IOptionsMonitor<CacheOptions> optionsMonitor) : ICacheProvider
{
  private readonly AsyncKeyedLock<string> _locks = new();

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
    var cached = await cache.GetStringAsync(fullKey, ct).ConfigureAwait(false);
    if (TryDeserialize(cached, out T? result))
    {
      return new CacheResult<T>(result!, IsHit: true);
    }

    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      cached = await cache.GetStringAsync(fullKey, ct).ConfigureAwait(false);
      if (TryDeserialize(cached, out result))
      {
        return new CacheResult<T>(result!, IsHit: true);
      }

      var value = await factory(ct).ConfigureAwait(false);
      await SetCoreAsync(fullKey, value, requestOptions, ct).ConfigureAwait(false);

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
      await SetCoreAsync(fullKey, value, requestOptions, ct).ConfigureAwait(false);
    }
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ct.ThrowIfCancellationRequested();

    var fullKey = ApplyPrefix(key);
    using (await _locks.AcquireAsync(fullKey, ct).ConfigureAwait(false))
    {
      await cache.RemoveAsync(fullKey, ct).ConfigureAwait(false);
    }
  }

  private string ApplyPrefix(string key) => $"{optionsMonitor.CurrentValue.KeyPrefix}{key}";

  private async Task SetCoreAsync<T>(
      string fullKey,
      T value,
      CacheOptions? requestOptions,
      CancellationToken ct)
  {
    var json = JsonSerializer.Serialize(value);
    await cache.SetStringAsync(
        fullKey,
        json,
        CreateDistributedCacheOptions(requestOptions),
        ct).ConfigureAwait(false);
  }

  private static bool TryDeserialize<T>(string? cached, out T? result)
  {
    result = default;
    if (cached is null)
    {
      return false;
    }

    try
    {
      // JSON "null" is a valid cached value and must be distinguishable from
      // a missing entry. A successful deserialize is therefore a hit even
      // when the resulting reference is null.
      result = JsonSerializer.Deserialize<T>(cached);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private DistributedCacheEntryOptions CreateDistributedCacheOptions(CacheOptions? requestOptions)
  {
    var options = requestOptions ?? optionsMonitor.CurrentValue;

    return new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = options.DefaultAbsoluteExpiration > TimeSpan.Zero
          ? options.DefaultAbsoluteExpiration
          : null,
      SlidingExpiration = options.DefaultSlidingExpiration > TimeSpan.Zero
          ? options.DefaultSlidingExpiration
          : null
    };
  }
}
