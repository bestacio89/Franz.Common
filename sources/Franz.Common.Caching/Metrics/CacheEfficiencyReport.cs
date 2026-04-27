using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Metrics;

public sealed record CacheEfficiencyReport
{
  public required string Key { get; init; }
  public CacheEfficiencyStatus Status { get; init; }
  public required string Reason { get; init; }
}