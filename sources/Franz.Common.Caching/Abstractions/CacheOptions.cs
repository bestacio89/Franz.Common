using Microsoft.Extensions.Caching.Memory;
using System;

namespace Franz.Common.Caching.Abstractions;

public sealed class CacheOptions
{
  /// <summary>
  /// Gets or sets an absolute expiration time, relative to now.
  /// </summary>
  public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }

  /// <summary>
  /// Gets or sets an absolute expiration date for the cache entry.
  /// </summary>
  public DateTimeOffset? AbsoluteExpiration { get; init; }

  /// <summary>
  /// Gets or sets how long a cache entry can be inactive (not accessed) before it will be removed.
  /// This will not extend the entry lifetime beyond the absolute expiration (if set).
  /// </summary>
  public TimeSpan? SlidingExpiration { get; init; }

  /// <summary>
  /// Legacy property for backwards compatibility. Maps to AbsoluteExpirationRelativeToNow.
  /// </summary>
  public TimeSpan? Expiration
  {
    get => AbsoluteExpirationRelativeToNow;
    init => AbsoluteExpirationRelativeToNow = value;
  }

  /// <summary>
  /// Time-to-live hint for the cache entry. Used by observers for logging/metrics.
  /// </summary>
  public TimeSpan? Ttl => AbsoluteExpirationRelativeToNow ?? SlidingExpiration;

  /// <summary>
  /// Hint for local in-memory caching duration (for hybrid cache scenarios).
  /// </summary>
  public TimeSpan? LocalCacheHint { get; init; }

  /// <summary>
  /// Tags associated with this cache entry for grouped invalidation.
  /// </summary>
  public string[]? Tags { get; init; }

  /// <summary>
  /// Priority of the cache entry (used by some cache providers).
  /// </summary>
  public CacheItemPriority? Priority { get; init; }

  /// <summary>
  /// Estimated size of the cache entry in bytes (for observers/metrics).
  /// </summary>
  public long? EstimatedSizeInBytes { get; init; }
}

/// <summary>
/// Specifies how items are prioritized for preservation during memory pressure.
/// </summary>
