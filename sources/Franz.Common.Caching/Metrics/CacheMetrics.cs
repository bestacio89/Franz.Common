using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Metrics;
public static class CacheMetrics
{
  public const string MeterName = "Franz.Caching";
  public static readonly Meter Meter = new(MeterName, "1.0.0");

  public static readonly Counter<long> Hits =
    Meter.CreateCounter<long>("franz_cache_hits", unit: "1",
      description: "Number of cache hits.");

  public static readonly Counter<long> Misses =
    Meter.CreateCounter<long>("franz_cache_misses", unit: "1",
      description: "Number of cache misses.");

  public static readonly Histogram<double> LookupLatencyMs =
    Meter.CreateHistogram<double>("franz_cache_lookup_latency_ms", unit: "ms",
      description: "Latency of cache lookups (hit/miss).");
}