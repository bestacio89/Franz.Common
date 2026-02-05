using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

/// <summary>
/// Describes a cache entry that was set.
/// </summary>
public sealed class CacheEntryDescriptor
{
  /// <summary>
  /// The cache key.
  /// </summary>
  public required string Key { get; init; }

  /// <summary>
  /// Estimated size of the cached value in bytes.
  /// </summary>
  public long EstimatedSizeInBytes { get; init; }

  /// <summary>
  /// Time-to-live for this cache entry.
  /// </summary>
  public TimeSpan? Ttl { get; init; }

  /// <summary>
  /// Tags associated with this cache entry.
  /// </summary>
  public string[] Tags { get; init; } = Array.Empty<string>();

  /// <summary>
  /// When this entry was set.
  /// </summary>
  public DateTimeOffset SetAt { get; init; } = DateTimeOffset.UtcNow;
}

