using System.Diagnostics;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Metrics;
using Microsoft.Extensions.Options;

namespace Franz.Common.Caching.Observability;

public sealed class ObservableCacheProvider : ICacheProvider, ICacheWriter
{
  private readonly ICacheProvider _inner;
  private readonly ICacheWriter? _writer;
  private readonly ICacheObserver[] _observers;
  private readonly IOptionsMonitor<CacheOptions>? _optionsMonitor;

  public ObservableCacheProvider(
      ICacheProvider inner,
      IEnumerable<ICacheObserver> observers,
      IOptionsMonitor<CacheOptions>? optionsMonitor = null)
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _writer = inner as ICacheWriter;
    _observers = observers?.ToArray() ?? Array.Empty<ICacheObserver>();
    _optionsMonitor = optionsMonitor;
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
      CacheMetrics.Hits.Add(1);
      Notify(observer => observer.OnCacheHit(new CacheAccessDescriptor
      {
        Key = key,
        AccessedAt = DateTimeOffset.UtcNow,
        LookupLatencyMs = elapsedMs
      }));
    }
    else
    {
      CacheMetrics.Misses.Add(1);
      NotifySet(key, options);
    }

    CacheMetrics.LookupLatencyMs.Record(elapsedMs);
    return result;
  }

  public async Task SetAsync<T>(
      string key,
      T value,
      CacheOptions? options = null,
      CancellationToken ct = default)
  {
    if (_writer is null)
      throw new NotSupportedException("The wrapped cache provider does not implement ICacheWriter.");

    await _writer.SetAsync(key, value, options, ct).ConfigureAwait(false);
    NotifySet(key, options);
  }

  public async Task RemoveAsync(string key, CancellationToken ct = default)
  {
    await _inner.RemoveAsync(key, ct).ConfigureAwait(false);
    Notify(observer => observer.OnCacheRemove(key));
  }

  private void NotifySet(string key, CacheOptions? options)
  {
    var effectiveOptions = options ?? _optionsMonitor?.CurrentValue;
    var ttl = effectiveOptions?.DefaultAbsoluteExpiration ?? TimeSpan.FromMinutes(60);
    if (ttl == TimeSpan.Zero && effectiveOptions?.DefaultSlidingExpiration > TimeSpan.Zero)
      ttl = effectiveOptions.DefaultSlidingExpiration;

    var entry = new CacheEntryDescriptor
    {
      Key = key,
      EstimatedSizeInBytes = effectiveOptions?.DefaultEstimatedSizeInBytes ?? 1024,
      Ttl = ttl
    };

    Notify(observer => observer.OnCacheSet(entry));
  }

  private void Notify(Action<ICacheObserver> notification)
  {
    foreach (var observer in _observers)
    {
      try { notification(observer); }
      catch (Exception exception)
      {
        Trace.TraceError("Cache observer {0} failed: {1}", observer.GetType().FullName, exception);
      }
    }
  }
}
