using System.Diagnostics;
using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Observability;

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

  public async Task<CacheResult<T>> GetOrSetAsync<T>(
      string key,
      Func<CancellationToken, Task<T>> factory,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    var timestamp = Stopwatch.GetTimestamp();
    var result = await _inner.GetOrSetAsync(key, factory, options, ct).ConfigureAwait(false);
    var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

    if (result.IsHit)
    {
      Notify(observer => observer.OnCacheHit(new CacheAccessDescriptor
      {
        Key = key,
        AccessedAt = DateTimeOffset.UtcNow,
        LookupLatencyMs = elapsedMs
      }));
    }
    else
    {
      NotifySet(key, options);
    }

    return result;
  }

  public async Task SetAsync<T>(
      string key,
      T value,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    await _inner.SetAsync(key, value, options, ct).ConfigureAwait(false);
    NotifySet(key, options);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    await _inner.RemoveAsync(key, ct).ConfigureAwait(false);
    Notify(observer => observer.OnCacheRemove(key));
  }

  private void NotifySet(string key, CacheOptions? options)
  {
    var entry = new CacheEntryDescriptor
    {
      Key = key,
      EstimatedSizeInBytes = options?.DefaultEstimatedSizeInBytes ?? 1024,
      Ttl = options?.DefaultAbsoluteExpiration ?? TimeSpan.FromMinutes(30)
    };

    Notify(observer => observer.OnCacheSet(entry));
  }

  private void Notify(Action<ICacheObserver> notification)
  {
    foreach (var observer in _observers)
    {
      try
      {
        notification(observer);
      }
      catch (Exception exception)
      {
        // Observer failures must not change the result of a cache operation.
        // Notifications are synchronous because ICacheObserver is a synchronous
        // contract; no unbounded Task.Run queue or unobserved exception is created.
        Trace.TraceError(
            "Cache observer {0} failed: {1}",
            observer.GetType().FullName,
            exception);
      }
    }
  }
}
