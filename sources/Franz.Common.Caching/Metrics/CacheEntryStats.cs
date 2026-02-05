using System;
using System.Collections.Generic;
using System.Text;


using System;
using System.Collections.Concurrent;


namespace Franz.Common.Caching.Metrics;
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
  /// Hit rate for this entry (hits / total accesses).
  /// </summary>
  public double HitRate => Sets == 0 ? 0 : (double)Hits / (Hits + Sets);
}