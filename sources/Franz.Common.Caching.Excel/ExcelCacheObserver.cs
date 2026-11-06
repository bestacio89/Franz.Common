using Franz.Common.Caching.Observability;
using OfficeOpenXml;
using System.Collections.Concurrent;

namespace Franz.Common.Caching.Observability.Observers;

/// <summary>
/// Optional EPPlus-backed export of cache observer statistics.
/// </summary>
public sealed class ExcelCacheObserver : ICacheObserver
{
  private readonly ConcurrentDictionary<string, CacheStats> _stats = new();

  public void OnCacheHit(CacheAccessDescriptor access)
  {
    if (string.IsNullOrEmpty(access.Key))
    {
      return;
    }

    var stats = _stats.GetOrAdd(access.Key, static _ => new CacheStats());
    stats.RecordHit(access.LookupLatencyMs);
  }

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    if (string.IsNullOrEmpty(entry.Key))
    {
      return;
    }

    var stats = _stats.GetOrAdd(entry.Key, static _ => new CacheStats());
    stats.RecordSet(entry.EstimatedSizeInBytes);
  }

  public void OnCacheRemove(string key)
  {
    _stats.TryRemove(key, out _);
  }

  public void OnCacheRemoveByTag(string tag)
  {
    if (string.IsNullOrEmpty(tag))
    {
      return;
    }

    foreach (var key in _stats.Keys.Where(key =>
                 key.Contains(tag, StringComparison.OrdinalIgnoreCase)))
    {
      _stats.TryRemove(key, out _);
    }
  }

  public void ExportToExcel(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    var snapshot = _stats
        .Select(pair => (Key: pair.Key, Stats: pair.Value.Snapshot()))
        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .ToArray();

    using var package = new ExcelPackage();
    var sheet = package.Workbook.Worksheets.Add("CacheMetrics");

    sheet.Cells[1, 1].Value = "Key";
    sheet.Cells[1, 2].Value = "Hits";
    sheet.Cells[1, 3].Value = "Sets";
    sheet.Cells[1, 4].Value = "AverageLatencyMs";
    sheet.Cells[1, 5].Value = "EstimatedSizeBytes";

    long totalHits = 0;
    long totalSets = 0;
    long totalLatencyMicroseconds = 0;
    long totalSize = 0;

    for (var index = 0; index < snapshot.Length; index++)
    {
      var row = index + 2;
      var (key, stats) = snapshot[index];

      sheet.Cells[row, 1].Value = key;
      sheet.Cells[row, 2].Value = stats.Hits;
      sheet.Cells[row, 3].Value = stats.Sets;
      sheet.Cells[row, 4].Value = stats.Hits > 0
          ? stats.TotalLatencyMicroseconds / 1000d / stats.Hits
          : 0d;
      sheet.Cells[row, 5].Value = stats.EstimatedSizeBytes;

      totalHits += stats.Hits;
      totalSets += stats.Sets;
      totalLatencyMicroseconds += stats.TotalLatencyMicroseconds;
      totalSize += stats.EstimatedSizeBytes;
    }

    var totalRow = snapshot.Length + 2;
    sheet.Cells[totalRow, 1].Value = "TOTAL";
    sheet.Cells[totalRow, 2].Value = totalHits;
    sheet.Cells[totalRow, 3].Value = totalSets;
    sheet.Cells[totalRow, 4].Value = totalHits > 0
        ? totalLatencyMicroseconds / 1000d / totalHits
        : 0d;
    sheet.Cells[totalRow, 5].Value = totalSize;

    package.SaveAs(new FileInfo(path));
  }

  private sealed class CacheStats
  {
    private long _hits;
    private long _sets;
    private long _totalLatencyMicroseconds;
    private long _estimatedSizeBytes;

    public void RecordHit(double? latencyMilliseconds)
    {
      Interlocked.Increment(ref _hits);

      if (latencyMilliseconds is >= 0)
      {
        var microseconds = (long)Math.Round(latencyMilliseconds.Value * 1000d);
        Interlocked.Add(ref _totalLatencyMicroseconds, microseconds);
      }
    }

    public void RecordSet(long estimatedSizeBytes)
    {
      Interlocked.Increment(ref _sets);
      Interlocked.Exchange(ref _estimatedSizeBytes, estimatedSizeBytes);
    }

    public CacheStatsSnapshot Snapshot()
        => new(
            Volatile.Read(ref _hits),
            Volatile.Read(ref _sets),
            Volatile.Read(ref _totalLatencyMicroseconds),
            Volatile.Read(ref _estimatedSizeBytes));
  }

  private readonly record struct CacheStatsSnapshot(
      long Hits,
      long Sets,
      long TotalLatencyMicroseconds,
      long EstimatedSizeBytes);
}
