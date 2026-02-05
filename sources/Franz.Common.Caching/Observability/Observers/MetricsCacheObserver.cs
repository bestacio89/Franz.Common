using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Franz.Common.Caching.Observability.Observers;

public  class MetricsCacheObserver : ICacheObserver
{
  private readonly ConcurrentDictionary<string, CacheEntryStats> _stats
      = new();

  // Keep track of removed tags for testing/inspection
  private readonly ConcurrentBag<string> _removedTags = new();

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    var stat = _stats.GetOrAdd(entry.Key, _ => new CacheEntryStats());
    stat.Sets++;
    stat.EstimatedSizeBytes = entry.EstimatedSizeInBytes;
    stat.LastSet = DateTime.UtcNow;
  }

  public void OnCacheHit(CacheAccessDescriptor access)
  {
    var stat = _stats.GetOrAdd(access.Key, _ => new CacheEntryStats());
    stat.Hits++;
    stat.LastAccess = DateTime.UtcNow;

    // Update OpenTelemetry metric
    CacheMetrics.Hits.Add(1);
    if (access.LookupLatencyMs.HasValue)
      CacheMetrics.LookupLatencyMs.Record(access.LookupLatencyMs.Value);
  }

  public void OnCacheRemove(string key)
  {
    _stats.TryRemove(key, out _);
  }

  public void OnCacheRemoveByTag(string tag)
  {
    foreach (var kvp in _stats)
    {
      if (kvp.Value.Tags.Contains(tag))
        _stats.TryRemove(kvp.Key, out _);
    }

    // Track removed tag for tests
    _removedTags.Add(tag);
  }

  public IReadOnlyDictionary<string, CacheEntryStats> Snapshot() => _stats;

  // Expose current keys for tests
  public IReadOnlyCollection<string> CurrentKeys => (IReadOnlyCollection<string>)_stats.Keys;

  // Expose removed tags for test assertions
  public IReadOnlyCollection<string> CurrentRemovedTags => _removedTags;
}
