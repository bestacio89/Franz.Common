using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Observability;

public sealed record CacheMemorySnapshot
{
  public long TotalSizeInBytes { get; init; }
  public int EntryCount { get; init; }
  public double HitRatio { get; init; }
  public double MissRatio { get; init; }
}

