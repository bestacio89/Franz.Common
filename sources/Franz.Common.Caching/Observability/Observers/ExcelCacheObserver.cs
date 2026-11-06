using Franz.Common.Caching.Observability;
using OfficeOpenXml;
using System.Collections.Concurrent;

namespace Franz.Common.Caching.Observability.Observers;

public sealed class ExcelCacheObserver : ICacheObserver
{
  private readonly ConcurrentDictionary<string, CacheStats> _stats = new();
  private readonly CacheStats _overflowStats = new();
  private readonly object _trackingGate = new();
  private readonly int _maxTrackedEntries;
  private int _trackedEntries;

  public ExcelCacheObserver() : this(10_000) { }
  public ExcelCacheObserver(int maxTrackedEntries)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTrackedEntries);
    _maxTrackedEntries = maxTrackedEntries;
  }

  public void OnCacheHit(CacheAccessDescriptor access)
  {
    if (string.IsNullOrWhiteSpace(access.Key)) return;
    GetOrAddStats(access.Key).RecordHit(access.LookupLatencyMs);
  }

  public void OnCacheSet(CacheEntryDescriptor entry)
  {
    if (string.IsNullOrWhiteSpace(entry.Key)) return;
    GetOrAddStats(entry.Key).RecordSet(entry.EstimatedSizeInBytes, entry.Tags);
  }

  public void OnCacheRemove(string key)
  {
    lock (_trackingGate)
      if (_stats.TryRemove(key, out _)) _trackedEntries--;
  }

  public void OnCacheRemoveByTag(string tag)
  {
    if (string.IsNullOrWhiteSpace(tag)) return;
    lock (_trackingGate)
    {
      foreach (var pair in _stats.ToArray())
        if (pair.Value.HasTag(tag) && _stats.TryRemove(pair.Key, out _))
          _trackedEntries--;
    }
  }

  public void ExportToExcel(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    var snapshot = _stats.Select(pair => (Key: pair.Key, Stats: pair.Value.Snapshot())).ToList();
    var overflow = _overflowStats.Snapshot();
    if (!overflow.IsEmpty) snapshot.Add(("[other keys]", overflow));
    snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

    using var package = new ExcelPackage();
    var sheet = package.Workbook.Worksheets.Add("CacheMetrics");
    sheet.Cells[1, 1].Value = "Key";
    sheet.Cells[1, 2].Value = "Hits";
    sheet.Cells[1, 3].Value = "Sets";
    sheet.Cells[1, 4].Value = "AverageLatencyMs";
    sheet.Cells[1, 5].Value = "EstimatedSizeBytes";

    long totalHits = 0, totalSets = 0, totalLatencyMicroseconds = 0, totalSize = 0;
    for (var index = 0; index < snapshot.Count; index++)
    {
      var row = index + 2;
      var (key, stats) = snapshot[index];
      sheet.Cells[row, 1].Value = key;
      sheet.Cells[row, 2].Value = stats.Hits;
      sheet.Cells[row, 3].Value = stats.Sets;
      sheet.Cells[row, 4].Value = stats.Hits > 0
          ? stats.TotalLatencyMicroseconds / 1000d / stats.Hits : 0d;
      sheet.Cells[row, 5].Value = stats.EstimatedSizeBytes;
      totalHits += stats.Hits;
      totalSets += stats.Sets;
      totalLatencyMicroseconds += stats.TotalLatencyMicroseconds;
      totalSize += stats.EstimatedSizeBytes;
    }

    var totalRow = snapshot.Count + 2;
    sheet.Cells[totalRow, 1].Value = "TOTAL";
    sheet.Cells[totalRow, 2].Value = totalHits;
    sheet.Cells[totalRow, 3].Value = totalSets;
    sheet.Cells[totalRow, 4].Value = totalHits > 0
        ? totalLatencyMicroseconds / 1000d / totalHits : 0d;
    sheet.Cells[totalRow, 5].Value = totalSize;
    package.SaveAs(new FileInfo(path));
  }

  private CacheStats GetOrAddStats(string key)
  {
    if (_stats.TryGetValue(key, out var existing)) return existing;
    lock (_trackingGate)
    {
      if (_stats.TryGetValue(key, out existing)) return existing;
      if (_trackedEntries >= _maxTrackedEntries) return _overflowStats;
      var created = new CacheStats();
      _stats[key] = created;
      _trackedEntries++;
      return created;
    }
  }

  private sealed class CacheStats
  {
    private readonly object _tagsGate = new();
    private readonly HashSet<string> _tags = new(StringComparer.Ordinal);
    private long _hits, _sets, _totalLatencyMicroseconds, _estimatedSizeBytes;

    public void RecordHit(double? latencyMilliseconds)
    {
      Interlocked.Increment(ref _hits);
      if (latencyMilliseconds is >= 0)
        Interlocked.Add(ref _totalLatencyMicroseconds, (long)Math.Round(latencyMilliseconds.Value * 1000d));
    }

    public void RecordSet(long estimatedSizeBytes, IEnumerable<string>? tags)
    {
      Interlocked.Increment(ref _sets);
      Interlocked.Exchange(ref _estimatedSizeBytes, estimatedSizeBytes);
      lock (_tagsGate)
      {
        _tags.Clear();
        if (tags is not null)
          foreach (var tag in tags) _tags.Add(tag);
      }
    }

    public bool HasTag(string tag)
    {
      lock (_tagsGate) return _tags.Contains(tag);
    }

    public CacheStatsSnapshot Snapshot()
        => new(Volatile.Read(ref _hits), Volatile.Read(ref _sets),
            Volatile.Read(ref _totalLatencyMicroseconds), Volatile.Read(ref _estimatedSizeBytes));
  }

  private readonly record struct CacheStatsSnapshot(
      long Hits, long Sets, long TotalLatencyMicroseconds, long EstimatedSizeBytes)
  {
    public bool IsEmpty => Hits == 0 && Sets == 0;
  }
}
