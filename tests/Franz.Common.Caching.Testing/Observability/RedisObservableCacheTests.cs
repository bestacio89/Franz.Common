using System;
using System.Threading.Tasks;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Testing.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Caching.Testing.Tests;

[CollectionDefinition("RedisCacheTests")]
public class RedisCacheCollection : ICollectionFixture<RedisCacheFixture> { }

[Collection("RedisCacheTests")]
public class RedisObservableCacheTests : IClassFixture<RedisCacheFixture>
{
  private readonly RedisCacheFixture _fixture;

  public RedisObservableCacheTests(RedisCacheFixture fixture)
  {
    _fixture = fixture;
    ResetObservers();
  }

  private ICacheProvider Cache => _fixture.ServiceProvider.GetRequiredService<ICacheProvider>();
  private MetricsCacheObserver MetricsObserver => _fixture.ServiceProvider.GetRequiredService<MetricsCacheObserver>();
  private LoggingMetricsObserver LoggingObserver => _fixture.ServiceProvider.GetRequiredService<LoggingMetricsObserver>();

  private void ResetObservers()
  {
    MetricsObserver.Reset();
    LoggingObserver.Reset();
  }

  [Fact]
  public async Task RedisCache_SetGet_HitTriggersObservers()
  {
    string key = $"integration_test_{Guid.NewGuid()}";

    // First call → MISS (set)
    var firstResult = await Cache.GetOrSetAsync(key, ct => Task.FromResult(123));
    Assert.Equal(123, firstResult.Value);
    Assert.False(firstResult.IsHit);
    Assert.Equal(1, MetricsObserver.TotalSets);
    Assert.Equal(1, LoggingObserver.TotalSets);

    // Second call → HIT
    var secondResult = await Cache.GetOrSetAsync(key, ct => Task.FromResult(456));
    Assert.Equal(123, secondResult.Value); // cached value
    Assert.True(secondResult.IsHit);
    Assert.Equal(1, MetricsObserver.TotalHits);
    Assert.Equal(1, LoggingObserver.TotalHits);

    await Cache.RemoveAsync(key);
  }

  [Fact]
  public async Task RedisCache_Remove_NotifiesObservers()
  {
    string key = $"remove_test_{Guid.NewGuid()}";

    await Cache.GetOrSetAsync(key, ct => Task.FromResult(999));
    Assert.Contains(key, MetricsObserver.CurrentKeys);
    Assert.Contains(key, LoggingObserver.CurrentKeys);

    await Cache.RemoveAsync(key);

    Assert.DoesNotContain(key, MetricsObserver.CurrentKeys);
    Assert.DoesNotContain(key, LoggingObserver.CurrentKeys);
    Assert.Equal(1, MetricsObserver.TotalRemovals);
    Assert.Equal(1, LoggingObserver.TotalRemovals);
  }

  [Fact]
  public async Task RedisCache_RemoveByTag_NotifiesObservers()
  {
    string key1 = $"tag_test_1_{Guid.NewGuid()}";
    string key2 = $"tag_test_2_{Guid.NewGuid()}";
    string tag = $"group_{Guid.NewGuid()}";

    await Cache.GetOrSetAsync(key1, ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
    await Cache.GetOrSetAsync(key2, ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });

    Assert.Equal(2, MetricsObserver.TotalSets);
    Assert.Equal(2, LoggingObserver.TotalSets);

    await Cache.RemoveByTagAsync(tag);

    Assert.DoesNotContain(key1, MetricsObserver.CurrentKeys);
    Assert.DoesNotContain(key2, MetricsObserver.CurrentKeys);
    Assert.DoesNotContain(key1, LoggingObserver.CurrentKeys);
    Assert.DoesNotContain(key2, LoggingObserver.CurrentKeys);

    Assert.Contains(tag, MetricsObserver.CurrentRemovedTags);
    Assert.Contains(tag, LoggingObserver.CurrentRemovedTags);
  }

  [Fact]
  public async Task RedisCache_MultipleObservers_BothReceiveNotifications()
  {
    string key = $"multi_observer_{Guid.NewGuid()}";

    var result = await Cache.GetOrSetAsync(key, ct => Task.FromResult("test_value"));
    Assert.False(result.IsHit);

    Assert.Equal(1, MetricsObserver.TotalSets);
    Assert.Equal(1, LoggingObserver.TotalSets);

    await Cache.RemoveAsync(key);
  }

  [Fact]
  public async Task RedisCache_ComplexWorkflow_ObserversTrackAllOperations()
  {
    string keyPrefix = $"workflow_{Guid.NewGuid()}";
    string tag = $"workflow_tag_{Guid.NewGuid()}";

    // 3 Sets
    await Cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
    await Cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });
    await Cache.GetOrSetAsync($"{keyPrefix}_3", ct => Task.FromResult(3));

    // 2 Hits
    var hit1 = await Cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(999));
    var hit2 = await Cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(999));

    Assert.True(hit1.IsHit);
    Assert.True(hit2.IsHit);

    // Removals
    await Cache.RemoveAsync($"{keyPrefix}_3");
    await Cache.RemoveByTagAsync(tag);

    Assert.Equal(3, MetricsObserver.TotalSets);
    Assert.Equal(3, LoggingObserver.TotalSets);
    Assert.Equal(2, MetricsObserver.TotalHits);
    Assert.Equal(2, LoggingObserver.TotalHits);
    Assert.Equal(1, MetricsObserver.TotalRemovals);
    Assert.Equal(1, LoggingObserver.TotalRemovals);
    Assert.Contains(tag, MetricsObserver.CurrentRemovedTags);
    Assert.Contains(tag, LoggingObserver.CurrentRemovedTags);
    Assert.Empty(MetricsObserver.CurrentKeys);
    Assert.Empty(LoggingObserver.CurrentKeys);
  }
}
