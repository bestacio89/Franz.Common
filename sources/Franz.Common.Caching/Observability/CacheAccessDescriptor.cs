using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

public sealed record CacheAccessDescriptor
{
  public string Key { get; init; }
  public CacheAccessResult Result { get; init; } // Hit / Miss
  public DateTimeOffset AccessedAt { get; init; }

  /// <summary>
  /// Optional: latency in milliseconds for this lookup.
  /// Can be used for histograms / performance metrics.
  /// </summary>
  public double? LookupLatencyMs { get; init; }

  /// <summary>
  /// Optional: size of the cached value accessed (in bytes), if available.
  /// </summary>
  public long? ValueSizeBytes { get; init; }

  /// <summary>
  /// Optional: any associated tags for this entry.
  /// Useful for tag-based metrics.
  /// </summary>
  public IReadOnlyList<string>? Tags { get; init; }
}

