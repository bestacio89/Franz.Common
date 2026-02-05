using Franz.Common.Caching.Observability.Observers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Observability;

public sealed class TestMetricsCacheObserver : MetricsCacheObserver
{
  public readonly System.Collections.Generic.List<string> RemovedTags = new();

  public void OnRemoveByTag(string tag)
  {
    base.OnCacheRemoveByTag(tag); // Keep normal behavior
    RemovedTags.Add(tag);          // Track for assertions
  }
}

