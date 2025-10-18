using Franz.Common.Caching.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Integration.Tests.Caching.Metrics;
public class CacheMetricsTests
{
  [Fact]
  public void Should_Record_Hits_And_Misses_Without_Exception()
  {
    CacheMetrics.Hits.Add(1);
    CacheMetrics.Misses.Add(1);
    CacheMetrics.LookupLatencyMs.Record(5.5);

    // No assertions needed — absence of exception is the test
    Assert.True(true);
  }
}