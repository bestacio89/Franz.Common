using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Franz.Common.Caching.Observability.Observers;

public sealed class MetricsCacheObserver : ICacheObserver
{
  private readonly ConcurrentDictionary<string, CacheEntryStats> _stats = new();
  private readonly ConcurrentBag<string> _removedTags = new();

  public int TotalSets { get; private set; }
  public int TotalHits { get; private set; }
  public int TotalRemovals { get; private set; }
  public IReadOnlyCollection<string> CurrentRemovedTags => _removedTags;
  public IReadOnlyCollection<string> CurrentKeys => _stats.Keys.ToList();

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    var stat = _stats.GetOrAdd(entry.Key, _ => new CacheEntryStats());
    stat.Sets++;
    stat.EstimatedSizeBytes = entry.EstimatedSizeInBytes;
    stat.LastSet = DateTime.UtcNow;
    TotalSets++;
  }

  public void OnCacheHit(CacheAccessDescriptor access)
  {
    var stat = _stats.GetOrAdd(access.Key, _ => new CacheEntryStats());
    stat.Hits++;
    stat.LastAccess = DateTime.UtcNow;
    TotalHits++;

    // Update OpenTelemetry metric if needed
    CacheMetrics.Hits.Add(1);
    if (access.LookupLatencyMs.HasValue)
      CacheMetrics.LookupLatencyMs.Record(access.LookupLatencyMs.Value);
  }

  public void OnCacheRemove(string key)
  {
    _stats.TryRemove(key, out _);
    TotalRemovals++;
  }

  public void OnCacheRemoveByTag(string tag)
  {
    foreach (var kvp in _stats)
    {
      if (kvp.Value.Tags.Contains(tag))
        _stats.TryRemove(kvp.Key, out _);
    }
    _removedTags.Add(tag);
  }
  public void Reset()
  {
    // If you are running tests in parallel, use lock() or Interlocked
    TotalSets = 0;
    TotalHits = 0;
    TotalRemovals = 0;
    CurrentKeys.Clear();
    CurrentRemovedTags.Clear();
  }
  public IReadOnlyDictionary<string, CacheEntryStats> Snapshot() => _stats;
}
