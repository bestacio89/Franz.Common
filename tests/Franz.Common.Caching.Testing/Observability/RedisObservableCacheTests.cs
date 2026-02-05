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

      // Use your extensions for clean DI setup
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

      Assert.DoesNotContain(key, metricsObserver.CurrentKeys);
    }

    [Fact]
    public async Task RedisCache_RemoveByTag_NotifiesObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();

      string key1 = "tag_test_1";
      string key2 = "tag_test_2";

      await cache.GetOrSetAsync($"tag:group1:{key1}", ct => Task.FromResult(1));
      await cache.GetOrSetAsync($"tag:group1:{key2}", ct => Task.FromResult(2));

      Assert.Contains($"tag:group1:{key1}", metricsObserver.CurrentKeys);
      Assert.Contains($"tag:group1:{key2}", metricsObserver.CurrentKeys);

      // RedisCacheProvider doesn't support tags natively; your observer handles the removal simulation
      await cache.RemoveByTagAsync("group1");

      Assert.DoesNotContain($"tag:group1:{key1}", metricsObserver.CurrentKeys);
      Assert.DoesNotContain($"tag:group1:{key2}", metricsObserver.CurrentKeys);
    }
  }
}
