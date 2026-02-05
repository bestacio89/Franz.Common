using System;
using System.Threading.Tasks;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Testing.Fixtures;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

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

    // Reset the singleton observers before every test runs
    // This ensures a clean state (0 sets, 0 hits) for every [Fact]
    MetricsObserver.Reset();
    LoggingObserver.Reset();
  }

  private ICacheProvider Cache => _fixture.ServiceProvider.GetRequiredService<ICacheProvider>();
  private MetricsCacheObserver MetricsObserver => _fixture.ServiceProvider.GetRequiredService<MetricsCacheObserver>();
  private LoggingMetricsObserver LoggingObserver => _fixture.ServiceProvider.GetRequiredService<LoggingMetricsObserver>();

  [Fact]
  public async Task RedisCache_SetGet_HitTriggersObservers()
  {
    string key = $"integration_test_{Guid.NewGuid()}";

    // First call: Set (Miss)
    await Cache.GetOrSetAsync(key, ct => Task.FromResult(123));
    Assert.Equal(1, MetricsObserver.TotalSets);

    // Second call: Hit
    var cachedValue = await Cache.GetOrSetAsync(key, ct => Task.FromResult(456));
    Assert.Equal(123, cachedValue);

    Assert.Contains(key, MetricsObserver.CurrentKeys);
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

    await Cache.RemoveAsync(key);

    Assert.DoesNotContain(key, MetricsObserver.CurrentKeys);
    Assert.Equal(1, MetricsObserver.TotalRemovals);
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

    await Cache.RemoveByTagAsync(tag);

    Assert.DoesNotContain(key1, MetricsObserver.CurrentKeys);
    Assert.DoesNotContain(key2, MetricsObserver.CurrentKeys);
    Assert.Contains(tag, MetricsObserver.CurrentRemovedTags);
  }

  [Fact]
  public async Task RedisCache_MultipleObservers_BothReceiveNotifications()
  {
    string key = $"multi_observer_{Guid.NewGuid()}";

    await Cache.GetOrSetAsync(key, ct => Task.FromResult("test_value"));

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
    await Cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(999));
    await Cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(999));

    // 1 Removal by key, 1 Removal by tag
    await Cache.RemoveAsync($"{keyPrefix}_3");
    await Cache.RemoveByTagAsync(tag);

    Assert.Equal(3, MetricsObserver.TotalSets);
    Assert.Equal(2, MetricsObserver.TotalHits);
    Assert.Equal(1, MetricsObserver.TotalRemovals);
    Assert.Contains(tag, MetricsObserver.CurrentRemovedTags);
    Assert.Empty(MetricsObserver.CurrentKeys);
  }
}