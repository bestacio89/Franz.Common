using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Observability.Observers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Observability;

/// <summary>
/// Decorator that wraps an ICacheProvider and notifies observers of cache operations.
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
  }

  public async Task<T?> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    var stopwatch = Stopwatch.StartNew();

    // Wrap the factory to detect if it was called (cache miss)
    bool factoryWasCalled = false;
    T? result = default;

    async Task<T> WrappedFactory(CancellationToken token)
    {
      factoryWasCalled = true;
      var value = await factory(token);

      // Notify observers of cache SET (miss + set)
      NotifySet(key, value, options);

      return value;
    }

    result = await _inner.GetOrSetAsync(key, WrappedFactory, options, ct);

    stopwatch.Stop();

    // If factory wasn't called, it was a cache HIT
    if (!factoryWasCalled)
    {
      NotifyHit(key, stopwatch.Elapsed.TotalMilliseconds);
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
        // Swallow observer exceptions to prevent cache operations from failing
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
      catch
      {
        // Swallow observer exceptions
      }
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
      catch
      {
        // Swallow observer exceptions
      }
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
      catch
      {
        // Swallow observer exceptions
      }
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

    return TimeSpan.FromMinutes(30); // Default
  }

  private static long EstimateSize<T>(T value)
  {
    if (value == null) return 0;

    // Simple estimation - in production you might want more sophisticated sizing
    var str = value.ToString();
    return str?.Length ?? 0;
  }

  #endregion
}