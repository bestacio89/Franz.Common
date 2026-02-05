using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Caching.Observability.Observers;

public class MetricsCacheObserver : ICacheObserver
{
  private readonly ConcurrentDictionary<string, CacheEntryStats> _stats = new();

  // Keep track of removed tags for testing/inspection
  private readonly ConcurrentBag<string> _removedTags = new();

  // Track operation counts for testing
  private int _totalSets = 0;
  private int _totalHits = 0;
  private int _totalRemovals = 0;

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    var stat = _stats.GetOrAdd(entry.Key, _ => new CacheEntryStats());
    stat.Sets++;
    stat.EstimatedSizeBytes = entry.EstimatedSizeInBytes;
    stat.LastSet = DateTime.UtcNow;

    // Increment total sets counter
    System.Threading.Interlocked.Increment(ref _totalSets);
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

    // Increment total hits counter
    System.Threading.Interlocked.Increment(ref _totalHits);
  }

  public void OnCacheRemove(string key)
  {
    _stats.TryRemove(key, out _);

    // Increment total removals counter
    System.Threading.Interlocked.Increment(ref _totalRemovals);
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

    // Increment total removals counter
    System.Threading.Interlocked.Increment(ref _totalRemovals);
  }

  public IReadOnlyDictionary<string, CacheEntryStats> Snapshot() => _stats;

  // Expose current keys for tests
  public IReadOnlyCollection<string> CurrentKeys => _stats.Keys.ToList();

  // Expose removed tags for test assertions
  public IReadOnlyCollection<string> CurrentRemovedTags => _removedTags.ToList();

  // Expose operation counts for testing
  public int TotalSets => _totalSets;
  public int TotalHits => _totalHits;
  public int TotalRemovals => _totalRemovals;
}