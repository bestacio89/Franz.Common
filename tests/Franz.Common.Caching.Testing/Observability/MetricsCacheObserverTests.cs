using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Xunit;

namespace Franz.Common.Caching.Tests.Observability;

public sealed class MetricsCacheObserverTests
{
  [Fact]
  public void PerKeyTracking_ShouldBeBounded_WhileAggregateCountersContinue()
  {
    var observer = new MetricsCacheObserver(maxTrackedEntries: 1);
    observer.OnCacheSet(new CacheEntryDescriptor { Key = "first", EstimatedSizeInBytes = 100, Tags = new[] { "customer" } });
    observer.OnCacheSet(new CacheEntryDescriptor { Key = "second", EstimatedSizeInBytes = 200 });
    observer.OnCacheHit(new CacheAccessDescriptor { Key = "second", AccessedAt = DateTimeOffset.UtcNow });

    Assert.Single(observer.CurrentKeys);
    Assert.Equal(2, observer.TotalSets);
    Assert.Equal(1, observer.TotalHits);
    Assert.Single(observer.Snapshot());
  }

  [Fact]
  public void PerKeyTagTracking_ShouldBeBounded()
  {
    var observer = new MetricsCacheObserver();
    var tags = Enumerable.Range(0, 100).Select(index => $"tag-{index}").ToArray();

    observer.OnCacheSet(new CacheEntryDescriptor
    {
      Key = "tagged",
      EstimatedSizeInBytes = 100,
      Tags = tags
    });

    Assert.Equal(64, observer.Snapshot()["tagged"].Tags.Count);
  }

  [Fact]
  public void ConcurrentUpdates_ShouldNotLosePerKeyHits()
  {
    var observer = new MetricsCacheObserver();
    Parallel.For(0, 1000, _ =>
        observer.OnCacheHit(new CacheAccessDescriptor { Key = "shared", AccessedAt = DateTimeOffset.UtcNow }));

    Assert.Equal(1000, observer.TotalHits);
    Assert.Equal(1000, observer.Snapshot()["shared"].Hits);
  }
}
