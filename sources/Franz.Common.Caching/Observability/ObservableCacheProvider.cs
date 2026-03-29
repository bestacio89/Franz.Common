using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
    // Use ValueStopwatch or a simple long-based timestamp for sub-ms precision if needed
    var timestamp = Stopwatch.GetTimestamp();

    var result = await _inner.GetOrSetAsync(key, factory, options, ct);

    var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

    if (result.IsHit)
    {
      NotifyHit(key, elapsedMs);
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

  [Obsolete("Tag-based invalidation is disabled in Franz.Common. Use key-based removal.")]
  public Task RemoveByTagAsync(string tag, CancellationToken ct = default)
  {
    // Fail fast to prevent hidden performance degradation
    throw new NotSupportedException("Tag-based observability is deprecated.");
  }

  #region Observer Notifications

  private void NotifySet<T>(string key, T value, CacheOptions? options)
  {
    if (_observers.Length == 0) return;

    var entry = new CacheEntryDescriptor
    {
      Key = key,
      // Leverage the new 'DefaultEstimatedSizeInBytes' from your CacheOptions
      EstimatedSizeInBytes = options?.DefaultEstimatedSizeInBytes ?? 1024,
      Ttl = options?.DefaultAbsoluteExpiration ?? TimeSpan.FromMinutes(30)
      // Tags removed: Clean Cereal approach.
    };

    // Fire-and-forget to avoid blocking the request thread. 
    // In a real Principal-level system, we would use a Channel<T> for background processing.
    _ = Task.Run(() =>
    {
      foreach (var observer in _observers)
      {
        try { observer.OnCacheSet(entry); } catch { /* Swallow to protect pipeline */ }
      }
    });
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

    _ = Task.Run(() =>
    {
      foreach (var observer in _observers)
      {
        try { observer.OnCacheHit(access); } catch { }
      }
    });
  }

  private void NotifyRemove(string key)
  {
    if (_observers.Length == 0) return;

    _ = Task.Run(() =>
    {
      foreach (var observer in _observers)
      {
        try { observer.OnCacheRemove(key); } catch { }
      }
    });
  }

  #endregion
}