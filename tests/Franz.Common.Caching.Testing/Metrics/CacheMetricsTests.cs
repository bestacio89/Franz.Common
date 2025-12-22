using FluentAssertions;
using Franz.Common.Caching.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Metrics;

public sealed class CacheMetricsTests
{
  [Fact]
  public void Meter_Should_Have_Correct_Name()
  {
    CacheMetrics.Meter.Name.Should().Be("Franz.Caching");
  }

  [Fact]
  public void Metrics_Should_Be_Initialized()
  {
    CacheMetrics.Hits.Should().NotBeNull();
    CacheMetrics.Misses.Should().NotBeNull();
    CacheMetrics.LookupLatencyMs.Should().NotBeNull();
  }

  [Fact]
  public void Counters_Should_Increment_Without_Exception()
  {
    CacheMetrics.Hits.Add(1);
    CacheMetrics.Misses.Add(1);
    CacheMetrics.LookupLatencyMs.Record(12.5);

    true.Should().BeTrue(); // no-op assertion: contract = no throw
  }
}