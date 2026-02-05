using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Observability;

/// <summary>
/// Decorator that wraps an ICacheProvider and notifies observers of cache operations.
/// Updated to support CacheResult<T>.
/// </summary>
public sealed class ObservableCacheProvider : ICacheProvider
{
  private readonly ICacheProvider _inner;
  private readonly ICacheObserver[] _observers;

  public ObservableCacheProvider(
      ICacheProvider inner,
      IEnumerable<ICacheObserver> observers)
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _observers = observers?.ToArray() ?? Array.Empty<ICacheObserver>();

    Debug.WriteLine($"ObservableCacheProvider created with {_observers.Length} observers");
  }

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    var stopwatch = Stopwatch.StartNew();

    var result = await _inner.GetOrSetAsync(key, factory, options, ct);

    stopwatch.Stop();

    if (result.IsHit)
    {
      NotifyHit(key, stopwatch.Elapsed.TotalMilliseconds);
    }
    else
    {
      NotifySet(key, result.Value, options);
    }

    return result;
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    await _inner.RemoveAsync(key, ct);
    NotifyRemove(key);
  }

  public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
  {
    await _inner.RemoveByTagAsync(tag, ct);
    NotifyRemoveByTag(tag);
  }

  #region Observer Notifications

  private void NotifySet<T>(string key, T value, CacheOptions? options)
  {
    if (_observers.Length == 0) return;

    var expiration = GetExpiration(options);

    var entry = new CacheEntryDescriptor
    {
      Key = key,
      EstimatedSizeInBytes = options?.EstimatedSizeInBytes ?? EstimateSize(value),
      Ttl = expiration,
      Tags = options?.Tags ?? Array.Empty<string>()
    };

    foreach (var observer in _observers)
    {
      try
      {
        observer.OnCacheSet(entry);
      }
      catch
      {
        // Swallow observer exceptions
      }
    }
  }

  private void NotifyHit(string key, double latencyMs)
  {
    if (_observers.Length == 0) return;

    var access = new CacheAccessDescriptor
    {
      Key = key,
      AccessedAt = DateTimeOffset.UtcNow,
      LookupLatencyMs = latencyMs
    };

    foreach (var observer in _observers)
    {
      try
      {
        observer.OnCacheHit(access);
      }
      catch { }
    }
  }

  private void NotifyRemove(string key)
  {
    if (_observers.Length == 0) return;

    foreach (var observer in _observers)
    {
      try
      {
        observer.OnCacheRemove(key);
      }
      catch { }
    }
  }

  private void NotifyRemoveByTag(string tag)
  {
    if (_observers.Length == 0) return;

    foreach (var observer in _observers)
    {
      try
      {
        observer.OnCacheRemoveByTag(tag);
      }
      catch { }
    }
  }

  private static TimeSpan GetExpiration(CacheOptions? options)
  {
    if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
      return options.AbsoluteExpirationRelativeToNow.Value;

    if (options?.AbsoluteExpiration.HasValue == true)
      return options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;

    if (options?.SlidingExpiration.HasValue == true)
      return options.SlidingExpiration.Value;

    return TimeSpan.FromMinutes(30);
  }

  private static long EstimateSize<T>(T value)
  {
    if (value == null) return 0;
    var str = value.ToString();
    return str?.Length ?? 0;
  }

  #endregion
}
