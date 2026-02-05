using Franz.Common.Caching.Observability;
using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Franz.Common.Caching.Observability.Observers
{
  public sealed class ExcelCacheObserver : ICacheObserver
  {
    // Thread-safe counters per key
    private readonly ConcurrentDictionary<string, CacheStats> _stats = new();

    public void OnCacheHit(CacheAccessDescriptor access)
    {
      if (!IsRelevant(access)) return;

      var stat = _stats.GetOrAdd(access.Key, _ => new CacheStats());
      stat.Hits++;
      if (access.LookupLatencyMs.HasValue)
        stat.TotalLatencyMs += access.LookupLatencyMs.Value;
    }

    public void OnCacheSet(CacheEntryDescriptor entry)
    {
      if (!IsRelevant(entry)) return;

      var stat = _stats.GetOrAdd(entry.Key, _ => new CacheStats());
      stat.Sets++;
      stat.EstimatedSizeBytes = entry.EstimatedSizeInBytes;
    }

    public void OnCacheRemove(string key)
    {
      _stats.TryRemove(key, out _);
    }

    public void OnCacheRemoveByTag(string tag)
    {
      var keys = _stats.Keys.Where(k => k.Contains(tag, StringComparison.OrdinalIgnoreCase)).ToList();
      foreach (var key in keys) _stats.TryRemove(key, out _);
    }

    // Simple relevance filter: only track keys used more than once
    private bool IsRelevant(CacheAccessDescriptor access) => access.Key.Length > 0;
    private bool IsRelevant(CacheEntryDescriptor entry) => entry.Key.Length > 0;

    // Export current stats to Excel
    public void ExportToExcel(string path)
    {
      using var package = new ExcelPackage();
      var sheet = package.Workbook.Worksheets.Add("CacheMetrics");

      // Header
      sheet.Cells[1, 1].Value = "Key";
      sheet.Cells[1, 2].Value = "Hits";
      sheet.Cells[1, 3].Value = "Sets";
      sheet.Cells[1, 4].Value = "AvgLatencyMs";
      sheet.Cells[1, 5].Value = "SizeBytes";

      int row = 2;
      long totalHits = 0;
      long totalSets = 0;
      double totalLatency = 0;
      long totalSize = 0;

      foreach (var kv in _stats)
      {
        var stat = kv.Value;
        sheet.Cells[row, 1].Value = kv.Key;
        sheet.Cells[row, 2].Value = stat.Hits;
        sheet.Cells[row, 3].Value = stat.Sets;
        sheet.Cells[row, 4].Value = stat.Hits > 0 ? stat.TotalLatencyMs / stat.Hits : 0;
        sheet.Cells[row, 5].Value = stat.EstimatedSizeBytes;

        totalHits += stat.Hits;
        totalSets += stat.Sets;
        totalLatency += stat.TotalLatencyMs;
        totalSize += stat.EstimatedSizeBytes;

        row++;
      }

      // Totals row
      sheet.Cells[row, 1].Value = "TOTAL";
      sheet.Cells[row, 2].Value = totalHits;
      sheet.Cells[row, 3].Value = totalSets;
      sheet.Cells[row, 4].Value = totalHits > 0 ? totalLatency / totalHits : 0;
      sheet.Cells[row, 5].Value = totalSize;

      package.SaveAs(new FileInfo(path));
    }


    private class CacheStats
    {
      public long Hits;
      public long Sets;
      public double TotalLatencyMs;
      public long EstimatedSizeBytes;
    }
  }
}
