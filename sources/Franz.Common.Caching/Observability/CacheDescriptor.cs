using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

public sealed record CacheEntryDescriptor
{
  public string Key { get; init; }
  public long EstimatedSizeInBytes { get; init; }
  public DateTimeOffset CreatedAt { get; init; }
  public TimeSpan? Ttl { get; init; }
  public string CacheRegion { get; init; }
}
