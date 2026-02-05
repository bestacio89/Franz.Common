using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Testing.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Caching.Testing.Tests
{
  [Collection("RedisCacheTests")]
  public class RedisObservableCacheTests
  {
    private readonly RedisCacheFixture _fixture;

    public RedisObservableCacheTests(RedisCacheFixture fixture)
    {
      _fixture = fixture;
    }

    private ICacheProvider Cache => _fixture.ServiceProvider.GetRequiredService<ICacheProvider>();
    private MetricsCacheObserver MetricsObserver => _fixture.ServiceProvider.GetRequiredService<MetricsCacheObserver>();
    private LoggingMetricsObserver LoggingObserver => _fixture.ServiceProvider.GetRequiredService<LoggingMetricsObserver>();

    [Fact]
    public async Task RedisCache_SetGet_HitTriggersObservers()
    {
      string key = $"integration_test_{Guid.NewGuid()}";

      var value = await Cache.GetOrSetAsync(key, ct => Task.FromResult(123));
      Assert.Equal(123, value);

      var cachedValue = await Cache.GetOrSetAsync(key, ct => Task.FromResult(456));
      Assert.Equal(123, cachedValue);

      // Observers now see the correct state
      Assert.Contains(key, MetricsObserver.CurrentKeys);
      Assert.Contains(key, LoggingObserver.CurrentKeys);
      Assert.True(MetricsObserver.TotalHits > 0);
      Assert.True(LoggingObserver.TotalHits > 0);

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
      Assert.True(MetricsObserver.TotalRemovals > 0);
    }

    [Fact]
    public async Task RedisCache_RemoveByTag_NotifiesObservers()
    {
      string key1 = $"tag_test_1_{Guid.NewGuid()}";
      string key2 = $"tag_test_2_{Guid.NewGuid()}";
      string tag = $"group_{Guid.NewGuid()}";

      await Cache.GetOrSetAsync(key1, ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
      await Cache.GetOrSetAsync(key2, ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });

      Assert.Contains(key1, MetricsObserver.CurrentKeys);
      Assert.Contains(key2, MetricsObserver.CurrentKeys);

      await Cache.RemoveByTagAsync(tag);

      Assert.DoesNotContain(key1, MetricsObserver.CurrentKeys);
      Assert.DoesNotContain(key2, MetricsObserver.CurrentKeys);
      Assert.Contains(tag, MetricsObserver.CurrentRemovedTags);
    }

    [Fact]
    public async Task RedisCache_MultipleObservers_BothReceiveNotifications()
    {
      string key = $"multi_observer_{Guid.NewGuid()}";

      int initialMetricsSets = MetricsObserver.TotalSets;
      int initialLoggingSets = LoggingObserver.TotalSets;

      await Cache.GetOrSetAsync(key, ct => Task.FromResult("test_value"));

      Assert.Equal(initialMetricsSets + 1, MetricsObserver.TotalSets);
      Assert.Equal(initialLoggingSets + 1, LoggingObserver.TotalSets);

      Assert.Contains(key, MetricsObserver.CurrentKeys);
      Assert.Contains(key, LoggingObserver.CurrentKeys);

      await Cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RedisCache_ComplexWorkflow_ObserversTrackAllOperations()
    {
      string keyPrefix = $"workflow_{Guid.NewGuid()}";
      string tag = $"workflow_tag_{Guid.NewGuid()}";

      await Cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
      await Cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });
      await Cache.GetOrSetAsync($"{keyPrefix}_3", ct => Task.FromResult(3));

      await Cache.GetOrSetAsync($"{keyPrefix}_1", ct => Task.FromResult(999));
      await Cache.GetOrSetAsync($"{keyPrefix}_2", ct => Task.FromResult(999));

      await Cache.RemoveAsync($"{keyPrefix}_3");
      await Cache.RemoveByTagAsync(tag);

      Assert.True(MetricsObserver.TotalSets >= 3);
      Assert.True(MetricsObserver.TotalHits >= 2);
      Assert.True(MetricsObserver.TotalRemovals >= 1);
      Assert.Contains(tag, MetricsObserver.CurrentRemovedTags);

      Assert.DoesNotContain($"{keyPrefix}_1", MetricsObserver.CurrentKeys);
      Assert.DoesNotContain($"{keyPrefix}_2", MetricsObserver.CurrentKeys);
      Assert.DoesNotContain($"{keyPrefix}_3", MetricsObserver.CurrentKeys);
    }
  }
}
