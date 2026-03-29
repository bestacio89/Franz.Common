#nullable enable
using Franz.Common.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Franz.Common.Caching.Distributed;

/// <summary>
/// Thread-safe distributed cache provider with automatic healing on deserialization failure.
/// </summary>
public sealed class DistributedCacheProvider(
    IDistributedCache cache,
    IOptionsMonitor<CacheOptions> optionsMonitor) : ICacheProvider, IDisposable
{
  private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? requestOptions = null,
      CancellationToken ct = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(factory);

    // 1. Optimistic Fast Path (No Lock)
    var cached = await cache.GetStringAsync(key, ct);
    if (TryDeserialize(cached, out T? result))
    {
      return new CacheResult<T>(result!, IsHit: true);
    }

    // 2. Cache Miss or Corrupt Data -> Prepare for healing
    var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    await semaphore.WaitAsync(ct);

    try
    {
      // 3. Double-Check Locking (Check cache again after acquiring lock)
      cached = await cache.GetStringAsync(key, ct);
      if (TryDeserialize(cached, out result))
      {
        return new CacheResult<T>(result!, IsHit: true);
      }

      // 4. Factory Execution (Heal from Source)
      var value = await factory(ct);
      var json = JsonSerializer.Serialize(value);
      var entryOptions = CreateDistributedCacheOptions(requestOptions);

      await cache.SetStringAsync(key, json, entryOptions, ct);

      return new CacheResult<T>(value, IsHit: false);
    }
    finally
    {
      semaphore.Release();
      // Optional: Clean up semaphores if memory pressure is a concern
    }
  }

  private static bool TryDeserialize<T>(string? cached, out T? result)
  {
    result = default;
    if (string.IsNullOrWhiteSpace(cached)) return false;

    try
    {
      result = JsonSerializer.Deserialize<T>(cached);
      return result is not null;
    }
    catch (JsonException)
    {
   
      return false;
    }
  }

  private DistributedCacheEntryOptions CreateDistributedCacheOptions(CacheOptions? requestOptions)
  {
    var globalSettings = optionsMonitor.CurrentValue;
    var options = requestOptions ?? globalSettings;

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

  public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);

  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
      => throw new NotSupportedException("Tag-based invalidation is not supported by DistributedCacheProvider.");

  public void Dispose()
  {
    foreach (var semaphore in _locks.Values)
    {
      semaphore.Dispose();
    }
    _locks.Clear();
  }
}