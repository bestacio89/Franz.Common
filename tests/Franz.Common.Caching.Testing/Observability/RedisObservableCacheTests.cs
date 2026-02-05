using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Testing.Fixtures;
using Microsoft.Extensions.DependencyInjection;
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

      // Setup DI with proper observer registration
      services.AddFranzRedisCaching(_fixture.ConnectionString)
              .AddLogging()
              .AddObservableCaching()
              .AddMetricsCacheObserver()
              .AddLoggingMetricsCacheObserver();

      _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task RedisCache_SetGet_HitTriggersObservers()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();
      var loggingMetricsObserver = _sp.GetRequiredService<LoggingMetricsObserver>();

      string key = $"integration_test_key_{System.Guid.NewGuid()}";

      // Act - First call should set the cache
      var value = await cache.GetOrSetAsync(key, ct => Task.FromResult(123));
      Assert.Equal(123, value);

      // Act - Second call should hit the cache
      var cachedValue = await cache.GetOrSetAsync(key, ct => Task.FromResult(456));
      Assert.Equal(123, cachedValue); // Should still be original value

      // Assert - Observers should have recorded the set and hit
      Assert.Contains(key, metricsObserver.CurrentKeys);
      Assert.Contains(key, loggingMetricsObserver.CurrentKeys);

      // Verify hit count increased
      Assert.True(metricsObserver.TotalHits > 0, "MetricsCacheObserver should have recorded cache hits");
      Assert.True(loggingMetricsObserver.TotalHits > 0, "LoggingMetricsObserver should have recorded cache hits");

      // Cleanup
      await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_Remove_NotifiesObservers()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key = $"remove_test_key_{System.Guid.NewGuid()}";

      // Act - Set a value
      await cache.GetOrSetAsync(key, ct => Task.FromResult(999));

      // Assert - Key should be in observer
      Assert.Contains(key, metricsObserver.CurrentKeys);

      // Act - Remove the key
      await cache.RemoveAsync(key);

      // Assert - Key should be removed from observer
      Assert.DoesNotContain(key, metricsObserver.CurrentKeys);
      Assert.True(metricsObserver.TotalRemovals > 0, "Should have recorded at least one removal");
    }

    [Fact]
    public async Task RedisCache_RemoveByTag_NotifiesObservers()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key1 = $"tag_test_1_{System.Guid.NewGuid()}";
      string key2 = $"tag_test_2_{System.Guid.NewGuid()}";
      string tag = $"group_{System.Guid.NewGuid()}";

      // Act - Set values with tags
      await cache.GetOrSetAsync(key1, ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync(key2, ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });

      // Assert - Keys should be in observer
      Assert.Contains(key1, metricsObserver.CurrentKeys);
      Assert.Contains(key2, metricsObserver.CurrentKeys);

      // Act - Remove by tag
      await cache.RemoveByTagAsync(tag);

      // Assert - Keys should be removed
      Assert.DoesNotContain(key1, metricsObserver.CurrentKeys);
      Assert.DoesNotContain(key2, metricsObserver.CurrentKeys);

      // Observer should have registered tag removal
      Assert.Contains(tag, metricsObserver.CurrentRemovedTags);
    }

    [Fact]
    public async Task RedisCache_MultipleObservers_BothReceiveNotifications()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();
      var loggingMetricsObserver = _sp.GetRequiredService<LoggingMetricsObserver>();

      string key = $"multi_observer_test_{System.Guid.NewGuid()}";
      int initialMetricsCount = metricsObserver.TotalSets;
      int initialLoggingMetricsCount = loggingMetricsObserver.TotalSets;

      // Act
      await cache.GetOrSetAsync(key, ct => Task.FromResult("test_value"));

      // Assert - Both observers should be notified
      Assert.Equal(initialMetricsCount + 1, metricsObserver.TotalSets);
      Assert.Equal(initialLoggingMetricsCount + 1, loggingMetricsObserver.TotalSets);
      Assert.Contains(key, metricsObserver.CurrentKeys);
      Assert.Contains(key, loggingMetricsObserver.CurrentKeys);

      // Cleanup
      await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_CacheMiss_DoesNotTriggerHit()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key = $"miss_test_{System.Guid.NewGuid()}";
      int initialHits = metricsObserver.TotalHits;

      // Act - First call (cache miss, will set)
      await cache.GetOrSetAsync(key, ct => Task.FromResult(42));

      // Assert - Should not have increased hit count (was a miss, then a set)
      Assert.Equal(initialHits, metricsObserver.TotalHits);
      Assert.True(metricsObserver.TotalSets > 0, "Should have recorded the set operation");

      // Cleanup
      await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_WithExpiration_ObserverTracksEntry()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key = $"expiration_test_{System.Guid.NewGuid()}";
      var options = new CacheOptions
      {
        AbsoluteExpirationRelativeToNow = System.TimeSpan.FromSeconds(60)
      };

      // Act
      await cache.GetOrSetAsync(key, ct => Task.FromResult("expiring_value"), options);

      // Assert
      Assert.Contains(key, metricsObserver.CurrentKeys);

      // Cleanup
      await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_RemoveNonExistentKey_ObserverHandlesGracefully()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string nonExistentKey = $"non_existent_{System.Guid.NewGuid()}";
      int initialRemovals = metricsObserver.TotalRemovals;

      // Act - Try to remove a key that doesn't exist
      await cache.RemoveAsync(nonExistentKey);

      // Assert - Observer should still track the removal attempt
      Assert.True(metricsObserver.TotalRemovals >= initialRemovals,
          "Observer should handle removal of non-existent keys gracefully");
    }

    [Fact]
    public async Task RedisCache_ComplexWorkflow_ObserversTrackAllOperations()
    {
      // Arrange
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string keyPrefix = $"workflow_{System.Guid.NewGuid()}";
      string tag = $"workflow_tag_{System.Guid.NewGuid()}";

      // Act - Complex workflow
      // 1. Set multiple values
      await cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(1),
          new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(2),
          new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync($"{keyPrefix}_3", ct => Task.FromResult(3));

      // 2. Hit the cache
      await cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(999));
      await cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(999));

      // 3. Remove one key
      await cache.RemoveAsync($"{keyPrefix}_3");

      // 4. Remove by tag
      await cache.RemoveByTagAsync(tag);

      // Assert
      Assert.True(metricsObserver.TotalSets >= 3, "Should have recorded at least 3 sets");
      Assert.True(metricsObserver.TotalHits >= 2, "Should have recorded at least 2 hits");
      Assert.True(metricsObserver.TotalRemovals >= 1, "Should have recorded at least 1 removal");
      Assert.Contains(tag, metricsObserver.CurrentRemovedTags);

      // All keys should be gone
      Assert.DoesNotContain($"{keyPrefix}_1", metricsObserver.CurrentKeys);
      Assert.DoesNotContain($"{keyPrefix}_2", metricsObserver.CurrentKeys);
      Assert.DoesNotContain($"{keyPrefix}_3", metricsObserver.CurrentKeys);
    }
  }
}