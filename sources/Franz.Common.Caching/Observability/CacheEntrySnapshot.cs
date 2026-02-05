using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

public sealed record CacheEntrySnapshot
{
  public string Key { get; init; }
  public long SizeInBytes { get; init; }
  public DateTimeOffset CreatedAt { get; init; }
  public TimeSpan? RemainingTtl { get; init; }
  public long HitCount { get; init; }
  public long MissCount { get; init; }
}
