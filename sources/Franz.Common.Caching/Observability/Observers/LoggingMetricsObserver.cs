using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Caching.Observability.Observers
{
  public sealed class LoggingMetricsObserver : ICacheObserver
  {
    private readonly ILogger<LoggingMetricsObserver> _logger;
    private readonly ConcurrentDictionary<string, bool> _keys = new();
    private readonly ConcurrentBag<string> _removedTags = new();

    // Track operation counts for testing
    private int _totalSets = 0;
    private int _totalHits = 0;
    private int _totalRemovals = 0;

    public LoggingMetricsObserver(ILogger<LoggingMetricsObserver> logger)
    {
      _logger = logger;
    }

    public void OnCacheHit(CacheAccessDescriptor access)
    {
      _keys.TryAdd(access.Key, true);

      if (_logger.IsEnabled(LogLevel.Debug))
      {
        _logger.LogDebug(
            "Cache HIT | Key={Key} | AccessedAt={AccessedAt} | LatencyMs={Latency}",
            access.Key,
            access.AccessedAt,
            access.LookupLatencyMs ?? 0);
      }

      // Metrics
      CacheMetrics.Hits.Add(1);
      if (access.LookupLatencyMs.HasValue)
        CacheMetrics.LookupLatencyMs.Record(access.LookupLatencyMs.Value);

      // Increment total hits counter
      System.Threading.Interlocked.Increment(ref _totalHits);
    }

    public void OnCacheSet(CacheEntryDescriptor entry)
    {
      _keys.TryAdd(entry.Key, true);

      if (_logger.IsEnabled(LogLevel.Information))
      {
        _logger.LogInformation(
            "Cache SET | Key={Key} | Size={SizeBytes} bytes | TTL={Ttl}",
            entry.Key,
            entry.EstimatedSizeInBytes,
            entry.Ttl);
      }

      // Metrics
      CacheMetrics.Misses.Add(1); // setting usually corresponds to a miss

      // Increment total sets counter
      System.Threading.Interlocked.Increment(ref _totalSets);
    }

    public void OnCacheRemove(string key)
    {
      _keys.TryRemove(key, out _);

      if (_logger.IsEnabled(LogLevel.Information))
      {
        _logger.LogInformation(
            "Cache REMOVE | Key={Key}",
            key);
      }

      // Increment total removals counter
      System.Threading.Interlocked.Increment(ref _totalRemovals);
    }

    public void OnCacheRemoveByTag(string tag)
    {
      foreach (var k in _keys.Keys.ToList())
      {
        if (k.Contains(tag))
          _keys.TryRemove(k, out _);
      }

      if (_logger.IsEnabled(LogLevel.Information))
      {
        _logger.LogInformation(
            "Cache REMOVE BY TAG | Tag={Tag}",
            tag);
      }

      // Track removed tag for tests
      _removedTags.Add(tag);

      // Increment total removals counter
      System.Threading.Interlocked.Increment(ref _totalRemovals);
    }

    // Expose keys for testing
    public IReadOnlyCollection<string> CurrentKeys => _keys.Keys.ToList();

    // Expose removed tags for test assertions
    public IReadOnlyCollection<string> CurrentRemovedTags => _removedTags.ToList();

    // Expose operation counts for testing
    public int TotalSets => _totalSets;
    public int TotalHits => _totalHits;
    public int TotalRemovals => _totalRemovals;
  }
}