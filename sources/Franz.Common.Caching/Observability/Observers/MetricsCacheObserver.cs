using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Franz.Common.Caching.Metrics;

namespace Franz.Common.Caching.Observability.Observers;

/// <summary>
/// Keeps a bounded diagnostic snapshot of per-key activity. Aggregate
/// instrument measurements are emitted once by ObservableCacheProvider;
/// high-cardinality keys beyond the configured limit are not retained individually.
/// </summary>
public sealed class MetricsCacheObserver : ICacheObserver
{
  private const int MaxRemovedTags = 256;
  private readonly ConcurrentDictionary<string, CacheEntryStats> _stats = new();
  private readonly ConcurrentQueue<string> _removedTags = new();
  private readonly object _trackingGate = new();
  private readonly int _maxTrackedEntries;
  private int _trackedEntries;
  private int _totalSets;
  private int _totalHits;
  private int _totalRemovals;

  public MetricsCacheObserver() : this(10_000) { }

  public MetricsCacheObserver(int maxTrackedEntries)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTrackedEntries);
    _maxTrackedEntries = maxTrackedEntries;
  }

  public int TotalSets => Volatile.Read(ref _totalSets);
  public int TotalHits => Volatile.Read(ref _totalHits);
  public int TotalRemovals => Volatile.Read(ref _totalRemovals);
  public List<string> CurrentRemovedTags => _removedTags.ToArray().ToList();
  public List<string> CurrentKeys => _stats.Keys.ToList();

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    GetOrAddStats(entry.Key)?.RecordSet(entry.EstimatedSizeInBytes, entry.Tags);
    Interlocked.Increment(ref _totalSets);
  }

  public void OnCacheHit(CacheAccessDescriptor access)
  {
    GetOrAddStats(access.Key)?.RecordHit();
    Interlocked.Increment(ref _totalHits);
  }

  public void OnCacheRemove(string key)
  {
    RemoveTrackedKey(key);
    Interlocked.Increment(ref _totalRemovals);
  }

  public void OnCacheRemoveByTag(string tag)
  {
    var removed = 0;
    lock (_trackingGate)
    {
      foreach (var pair in _stats.ToArray())
      {
        if (pair.Value.Tags.Contains(tag) && _stats.TryRemove(pair.Key, out _))
        {
          _trackedEntries--;
          removed++;
        }
      }
    }

    if (!string.IsNullOrWhiteSpace(tag))
    {
      _removedTags.Enqueue(tag);
      while (_removedTags.Count > MaxRemovedTags) _removedTags.TryDequeue(out _);
    }

    if (removed > 0) Interlocked.Add(ref _totalRemovals, removed);
  }

  public void Reset()
  {
    lock (_trackingGate)
    {
      _stats.Clear();
      _trackedEntries = 0;
      while (_removedTags.TryDequeue(out _)) { }
    }

    Interlocked.Exchange(ref _totalSets, 0);
    Interlocked.Exchange(ref _totalHits, 0);
    Interlocked.Exchange(ref _totalRemovals, 0);
  }

  public IReadOnlyDictionary<string, CacheEntryStats> Snapshot()
  {
    var snapshot = _stats.ToDictionary(pair => pair.Key, pair => pair.Value.Snapshot());
    return new ReadOnlyDictionary<string, CacheEntryStats>(snapshot);
  }

  private CacheEntryStats? GetOrAddStats(string key)
  {
    if (_stats.TryGetValue(key, out var existing)) return existing;

    lock (_trackingGate)
    {
      if (_stats.TryGetValue(key, out existing)) return existing;
      if (_trackedEntries >= _maxTrackedEntries) return null;

      var created = new CacheEntryStats();
      _stats[key] = created;
      _trackedEntries++;
      return created;
    }
  }

  private void RemoveTrackedKey(string key)
  {
    lock (_trackingGate)
    {
      if (_stats.TryRemove(key, out _)) _trackedEntries--;
    }
  }
}
