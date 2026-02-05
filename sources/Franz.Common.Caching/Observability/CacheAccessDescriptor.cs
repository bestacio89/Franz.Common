using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

/// <summary>
/// Describes a cache access (hit).
/// </summary>
public sealed class CacheAccessDescriptor
{
  /// <summary>
  /// The cache key that was accessed.
  /// </summary>
  public required string Key { get; init; }

  /// <summary>
  /// When the cache was accessed.
  /// </summary>
  public DateTimeOffset AccessedAt { get; init; } = DateTimeOffset.UtcNow;

  /// <summary>
  /// Optional latency of the lookup operation in milliseconds.
  /// </summary>
  public double? LookupLatencyMs { get; init; }
}


