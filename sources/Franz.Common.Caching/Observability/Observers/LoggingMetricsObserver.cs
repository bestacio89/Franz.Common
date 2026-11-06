using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Franz.Common.Caching.Observability.Observers
{
  public sealed class LoggingMetricsObserver : ICacheObserver
  {
    private const int MaxTrackedKeys = 10_000;
    private const int MaxRemovedTags = 256;
    private const int MaxTagsPerKey = 64;

    private readonly ILogger<LoggingMetricsObserver> _logger;
    private readonly ConcurrentDictionary<string, bool> _keys = new();
    private readonly ConcurrentQueue<string> _removedTags = new();
    private readonly ConcurrentDictionary<string, string[]> _keyTags = new();
    private readonly object _keyGate = new();

    private int _totalSets;
    private int _totalHits;
    private int _totalRemovals;

    public LoggingMetricsObserver(ILogger<LoggingMetricsObserver> logger)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnCacheHit(CacheAccessDescriptor access)
    {
      TrackKey(access.Key);
      if (_logger.IsEnabled(LogLevel.Debug))
        _logger.LogDebug("Cache HIT | Key={Key} | AccessedAt={AccessedAt} | LatencyMs={Latency}",
            access.Key, access.AccessedAt, access.LookupLatencyMs ?? 0);

      CacheMetrics.Hits.Add(1);
      if (access.LookupLatencyMs.HasValue)
        CacheMetrics.LookupLatencyMs.Record(access.LookupLatencyMs.Value);
      Interlocked.Increment(ref _totalHits);
    }

    public void OnCacheSet(CacheEntryDescriptor entry)
    {
      TrackKey(entry.Key, entry.Tags);
      if (_logger.IsEnabled(LogLevel.Information))
        _logger.LogInformation("Cache SET | Key={Key} | Size={SizeBytes} bytes | TTL={Ttl}",
            entry.Key, entry.EstimatedSizeInBytes, entry.Ttl);

      CacheMetrics.Misses.Add(1);
      Interlocked.Increment(ref _totalSets);
    }

    public void OnCacheRemove(string key)
    {
      RemoveKey(key);
      if (_logger.IsEnabled(LogLevel.Information))
        _logger.LogInformation("Cache REMOVE | Key={Key}", key);
      Interlocked.Increment(ref _totalRemovals);
    }

    public void OnCacheRemoveByTag(string tag)
    {
      lock (_keyGate)
      {
        foreach (var pair in _keyTags.ToArray())
        {
          if (pair.Value.Contains(tag, StringComparer.Ordinal))
          {
            _keyTags.TryRemove(pair.Key, out _);
            _keys.TryRemove(pair.Key, out _);
          }
        }
      }

      if (!string.IsNullOrWhiteSpace(tag))
      {
        _removedTags.Enqueue(tag);
        while (_removedTags.Count > MaxRemovedTags)
          _removedTags.TryDequeue(out _);
      }

      if (_logger.IsEnabled(LogLevel.Information))
        _logger.LogInformation("Cache REMOVE BY TAG | Tag={Tag}", tag);
      Interlocked.Increment(ref _totalRemovals);
    }

    public void Reset()
    {
      Interlocked.Exchange(ref _totalSets, 0);
      Interlocked.Exchange(ref _totalHits, 0);
      Interlocked.Exchange(ref _totalRemovals, 0);
      lock (_keyGate)
      {
        _keys.Clear();
        _keyTags.Clear();
      }
      while (_removedTags.TryDequeue(out _)) { }
    }

    private void TrackKey(string key, IEnumerable<string>? tags = null)
    {
      if (string.IsNullOrWhiteSpace(key)) return;
      lock (_keyGate)
      {
        if (!_keys.ContainsKey(key) && _keys.Count >= MaxTrackedKeys) return;
        _keys[key] = true;

        if (tags is null)
        {
          _keyTags.TryRemove(key, out _);
        }
        else
        {
          var boundedTags = new List<string>(MaxTagsPerKey);
          var seenTags = new HashSet<string>(StringComparer.Ordinal);
          var inspected = 0;
          foreach (var tag in tags)
          {
            if (inspected++ >= MaxTagsPerKey) break;
            if (!string.IsNullOrWhiteSpace(tag) && seenTags.Add(tag))
              boundedTags.Add(tag);
          }
          _keyTags[key] = boundedTags.ToArray();
        }
      }
    }

    private void RemoveKey(string key)
    {
      lock (_keyGate)
      {
        _keys.TryRemove(key, out _);
        _keyTags.TryRemove(key, out _);
      }
    }

    public int TotalSets => Volatile.Read(ref _totalSets);
    public int TotalHits => Volatile.Read(ref _totalHits);
    public int TotalRemovals => Volatile.Read(ref _totalRemovals);
    public IReadOnlyCollection<string> CurrentKeys => _keys.Keys.ToList();
    public IReadOnlyCollection<string> CurrentRemovedTags => _removedTags.ToArray();
  }
}
