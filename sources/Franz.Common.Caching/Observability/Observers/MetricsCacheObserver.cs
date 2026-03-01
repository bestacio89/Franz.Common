using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Franz.Common.Caching.Observability.Observers;

public sealed class MetricsCacheObserver : ICacheObserver
{
  private readonly ConcurrentDictionary<string, CacheEntryStats> _stats = new();
  private ConcurrentBag<string> _removedTags = new();

  public int TotalSets { get; private set; }
  public int TotalHits { get; private set; }
  public int TotalRemovals { get; private set; }

  public List<string> CurrentRemovedTags => _removedTags.ToList();
  public List<string> CurrentKeys => _stats.Keys.ToList();

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    var stat = _stats.GetOrAdd(entry.Key, _ => new CacheEntryStats());
    stat.Sets++;
    stat.EstimatedSizeBytes = entry.EstimatedSizeInBytes;
    stat.LastSet = DateTime.UtcNow;

    // Clear and store tags properly
    stat.Tags.Clear();
    if (entry.Tags != null)
    {
      foreach (var t in entry.Tags)
        stat.Tags.Add(t);
    }

    TotalSets++;
  }

  public void OnCacheHit(CacheAccessDescriptor access)
  {
    var stat = _stats.GetOrAdd(access.Key, _ => new CacheEntryStats());
    stat.Hits++;
    stat.LastAccess = DateTime.UtcNow;
    TotalHits++;

    // Optional metrics
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
    var keysToRemove = _stats
        .Where(kvp => kvp.Value.Tags.Contains(tag))
        .Select(kvp => kvp.Key)
        .ToList();

    foreach (var key in keysToRemove)
    {
      _stats.TryRemove(key, out _);
      TotalRemovals++;
    }

    _removedTags.Add(tag);
  }

  public void Reset()
  {
    TotalSets = 0;
    TotalHits = 0;
    TotalRemovals = 0;
    _stats.Clear();
    _removedTags = new ConcurrentBag<string>();
  }

  public IReadOnlyDictionary<string, CacheEntryStats> Snapshot() => _stats;
}