using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Testing.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Testing.Tests
{
  [CollectionDefinition("RedisCacheTests")]
  public class RedisCacheCollection : ICollectionFixture<RedisCacheFixture> { }

  [Collection("RedisCacheTests")]
  public class RedisObservableCacheTests
  {
    private readonly RedisCacheFixture _fixture;
    private readonly ServiceProvider _sp;

    public RedisObservableCacheTests(RedisCacheFixture fixture)
    {
      _fixture = fixture;

      var services = new ServiceCollection();

      // Observers MUST be registered before the decorator
      services.AddFranzRedisCaching(_fixture.ConnectionString)
              .AddLogging()
              .AddMetricsCacheObserver()
              .AddLoggingMetricsCacheObserver()
              .AddObservableCaching();

      _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task RedisCache_SetGet_HitTriggersObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();
      var loggingMetricsObserver = _sp.GetRequiredService<LoggingMetricsObserver>();

      string key = $"integration_test_{Guid.NewGuid()}";

      var value = await cache.GetOrSetAsync(key, ct => Task.FromResult(123));
      Assert.Equal(123, value);

      var cachedValue = await cache.GetOrSetAsync(key, ct => Task.FromResult(456));
      Assert.Equal(123, cachedValue); // Should still be original

      Assert.Contains(key, metricsObserver.CurrentKeys);
      Assert.Contains(key, loggingMetricsObserver.CurrentKeys);
      Assert.True(metricsObserver.TotalHits > 0);
      Assert.True(loggingMetricsObserver.TotalHits > 0);

      await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_Remove_NotifiesObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key = $"remove_test_{Guid.NewGuid()}";

      await cache.GetOrSetAsync(key, ct => Task.FromResult(999));
      Assert.Contains(key, metricsObserver.CurrentKeys);

      await cache.RemoveAsync(key);
      Assert.DoesNotContain(key, metricsObserver.CurrentKeys);
      Assert.True(metricsObserver.TotalRemovals > 0);
    }

    [Fact]
    public async Task RedisCache_RemoveByTag_NotifiesObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key1 = $"tag_test_1_{Guid.NewGuid()}";
      string key2 = $"tag_test_2_{Guid.NewGuid()}";
      string tag = $"group_{Guid.NewGuid()}";

      await cache.GetOrSetAsync(key1, ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync(key2, ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });

      Assert.Contains(key1, metricsObserver.CurrentKeys);
      Assert.Contains(key2, metricsObserver.CurrentKeys);

      await cache.RemoveByTagAsync(tag);

      Assert.DoesNotContain(key1, metricsObserver.CurrentKeys);
      Assert.DoesNotContain(key2, metricsObserver.CurrentKeys);
      Assert.Contains(tag, metricsObserver.CurrentRemovedTags);
    }

    [Fact]
    public async Task RedisCache_MultipleObservers_BothReceiveNotifications()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();
      var loggingMetricsObserver = _sp.GetRequiredService<LoggingMetricsObserver>();

      string key = $"multi_observer_{Guid.NewGuid()}";

      int initialMetricsSets = metricsObserver.TotalSets;
      int initialLoggingSets = loggingMetricsObserver.TotalSets;

      await cache.GetOrSetAsync(key, ct => Task.FromResult("test_value"));

      Assert.Equal(initialMetricsSets + 1, metricsObserver.TotalSets);
      Assert.Equal(initialLoggingSets + 1, loggingMetricsObserver.TotalSets);

      Assert.Contains(key, metricsObserver.CurrentKeys);
      Assert.Contains(key, loggingMetricsObserver.CurrentKeys);

      await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_ComplexWorkflow_ObserversTrackAllOperations()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string keyPrefix = $"workflow_{Guid.NewGuid()}";
      string tag = $"workflow_tag_{Guid.NewGuid()}";

      await cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync($"{keyPrefix}_3", ct => Task.FromResult(3));

      await cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(999));
      await cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(999));

      await cache.RemoveAsync($"{keyPrefix}_3");
      await cache.RemoveByTagAsync(tag);

      Assert.True(metricsObserver.TotalSets >= 3);
      Assert.True(metricsObserver.TotalHits >= 2);
      Assert.True(metricsObserver.TotalRemovals >= 1);
      Assert.Contains(tag, metricsObserver.CurrentRemovedTags);

      Assert.DoesNotContain($"{keyPrefix}_1", metricsObserver.CurrentKeys);
      Assert.DoesNotContain($"{keyPrefix}_2", metricsObserver.CurrentKeys);
      Assert.DoesNotContain($"{keyPrefix}_3", metricsObserver.CurrentKeys);
    }
  }
}
