using System;
using System.Collections.Concurrent;

namespace Franz.Common.Caching.Metrics
{
  /// <summary>
  /// Statistics for a single cache entry.
  /// </summary>
  public sealed class CacheEntryStats
  {
    /// <summary>
    /// Number of times this entry was set/written.
    /// </summary>
    public int Sets { get; set; }

    /// <summary>
    /// Number of times this entry was accessed (cache hits).
    /// </summary>
    public int Hits { get; set; }

    /// <summary>
    /// Last time this entry was set.
    /// </summary>
    public DateTime? LastSet { get; set; }

    /// <summary>
    /// Last time this entry was accessed.
    /// </summary>
    public DateTime? LastAccess { get; set; }

    /// <summary>
    /// Estimated size of this entry in bytes.
    /// </summary>
    public long EstimatedSizeBytes { get; set; }

    /// <summary>
    /// Tags associated with this entry.
    /// </summary>
    public ConcurrentBag<string> Tags { get; } = new();

    /// <summary>
    /// Hit rate for this entry (hits / (hits + sets)), safely handles zero.
    /// </summary>
    public double HitRate => (Hits + Sets) == 0 ? 0 : (double)Hits / (Hits + Sets);

    /// <summary>
    /// Add one or multiple tags to this entry.
    /// </summary>
    /// <param name="tags">Tags to associate</param>
    public void AddTags(IEnumerable<string>? tags)
    {
      if (tags == null) return;
      foreach (var t in tags)
        Tags.Add(t);
    }

    /// <summary>
    /// Clears all stats for this entry.
    /// </summary>
    public void Reset()
    {
      Sets = 0;
      Hits = 0;
      LastSet = null;
      LastAccess = null;
      EstimatedSizeBytes = 0;
      Tags.Clear();
    }
  }
}