using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Franz.Common.Caching.Observability.Observers
{
  public sealed class LoggingMetricsObserver : ICacheObserver
  {
    private readonly ILogger<LoggingMetricsObserver> _logger;
    private readonly ConcurrentDictionary<string, bool> _keys = new();

    public LoggingMetricsObserver(ILogger<LoggingMetricsObserver> logger)
    {
      _logger = logger;
    }

    public void OnCacheHit(CacheAccessDescriptor access)
    {
      _keys.TryAdd(access.Key, true);

      if (!_logger.IsEnabled(LogLevel.Information))
        return;

      // Logging
      _logger.LogDebug(
          "Cache HIT | Key={Key} | AccessedAt={AccessedAt} | LatencyMs={Latency}",
          access.Key,
          access.AccessedAt,
          access.LookupLatencyMs ?? 0);

      // Metrics
      CacheMetrics.Hits.Add(1);
      if (access.LookupLatencyMs.HasValue)
        CacheMetrics.LookupLatencyMs.Record(access.LookupLatencyMs.Value);
    }

    public void OnCacheSet(CacheEntryDescriptor entry)
    {
      _keys.TryAdd(entry.Key, true);

      if (!_logger.IsEnabled(LogLevel.Information))
        return;

      // Logging
      _logger.LogInformation(
          "Cache SET | Key={Key} | Size={SizeBytes} bytes | TTL={Ttl}",
          entry.Key,
          entry.EstimatedSizeInBytes,
          entry.Ttl);

      // Metrics
      CacheMetrics.Misses.Add(1); // setting usually corresponds to a miss
    }

    public void OnCacheRemove(string key)
    {
      _keys.TryRemove(key, out _);

      if (!_logger.IsEnabled(LogLevel.Information))
        return;

      _logger.LogInformation(
          "Cache REMOVE | Key={Key}",
          key);
    }

    public void OnCacheRemoveByTag(string tag)
    {
      foreach (var k in _keys.Keys)
      {
        if (k.Contains(tag))
          _keys.TryRemove(k, out _);
      }

      if (!_logger.IsEnabled(LogLevel.Information))
        return;

      _logger.LogInformation(
          "Cache REMOVE BY TAG | Tag={Tag}",
          tag);
    }

    // <-- expose keys for testing
    public IReadOnlyCollection<string> CurrentKeys => _keys.Keys.ToList();
  }
}
