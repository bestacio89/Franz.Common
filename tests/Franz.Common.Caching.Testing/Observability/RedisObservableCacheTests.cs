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

      // Clean DI setup
      services.AddFranzRedisCaching(_fixture.ConnectionString)
              .AddObservableCaching()
              .AddMetricsCacheObserver()
              .AddLoggingMetricsCacheObserver();

      _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task RedisCache_SetGet_HitTriggersObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();
      var loggingMetricsObserver = _sp.GetRequiredService<LoggingMetricsObserver>();

      string key = "integration_test_key";

      var value = await cache.GetOrSetAsync(key, ct => Task.FromResult(123));
      Assert.Equal(123, value);

      var cachedValue = await cache.GetOrSetAsync(key, ct => Task.FromResult(456));
      Assert.Equal(123, cachedValue);

      // Observers should have recorded the set and hit
      Assert.Contains(key, metricsObserver.CurrentKeys);
      Assert.Contains(key, loggingMetricsObserver.CurrentKeys);
    }

    [Fact]
    public async Task RedisCache_Remove_NotifiesObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key = "remove_test_key";

      await cache.GetOrSetAsync(key, ct => Task.FromResult(999));
      Assert.Contains(key, metricsObserver.CurrentKeys);

      await cache.RemoveAsync(key);

      // Key should be removed in observer
      Assert.DoesNotContain(key, metricsObserver.CurrentKeys);
    }

    [Fact]
    public async Task RedisCache_RemoveByTag_NotifiesObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key1 = "tag_test_1";
      string key2 = "tag_test_2";
      string tag = "group1";

      // Use native tag support in CacheOptions
      await cache.GetOrSetAsync(key1, ct => Task.FromResult(1), new CacheOptions { Tags = new[] { tag } });
      await cache.GetOrSetAsync(key2, ct => Task.FromResult(2), new CacheOptions { Tags = new[] { tag } });

      Assert.Contains(key1, metricsObserver.CurrentKeys);
      Assert.Contains(key2, metricsObserver.CurrentKeys);

      // Remove by tag
      await cache.RemoveByTagAsync(tag);

      // Keys should be removed
      Assert.DoesNotContain(key1, metricsObserver.CurrentKeys);
      Assert.DoesNotContain(key2, metricsObserver.CurrentKeys);

      // Observer should have registered tag removal
      Assert.Contains(tag, metricsObserver.CurrentRemovedTags);
    }
  }
}
