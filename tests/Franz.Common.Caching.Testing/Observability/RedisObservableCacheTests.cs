using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Redis;
using Franz.Common.Caching.Testing.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Linq;
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
      var multiplexer = ConnectionMultiplexer.Connect(_fixture.ConnectionString);
      var services = new ServiceCollection();
      services.AddLogging();

      // Redis cache provider
      services.AddSingleton<ICacheProvider>(sp =>
          new RedisCacheProvider(multiplexer));

      // Observers
      services.AddSingleton<MetricsCacheObserver>();
      services.AddSingleton<LoggingCacheObserver>();
      services.AddSingleton<LoggingMetricsObserver>();

      services.TryAddEnumerable(
          ServiceDescriptor.Singleton<ICacheObserver>(sp => sp.GetRequiredService<MetricsCacheObserver>())
      );
      services.TryAddEnumerable(
          ServiceDescriptor.Singleton<ICacheObserver>(sp => sp.GetRequiredService<LoggingCacheObserver>())
      );
      services.TryAddEnumerable(
          ServiceDescriptor.Singleton<ICacheObserver>(sp => sp.GetRequiredService<LoggingMetricsObserver>())
      );

      _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task RedisCache_SetGet_HitTriggersObservers()
    {
      var cache = _sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = _sp.GetRequiredService<MetricsCacheObserver>();
      var loggingMetricsObserver = _sp.GetRequiredService<LoggingMetricsObserver>();

      string key = "integration_test_key";

      // First set -> should be a cache miss internally
      var value = await cache.GetOrSetAsync(key, ct => Task.FromResult(123));
      Assert.Equal(123, value);

      // Second get -> should hit cache
      var cachedValue = await cache.GetOrSetAsync(key, ct => Task.FromResult(456));
      Assert.Equal(123, cachedValue); // value unchanged

      // Verify observers tracked the hit
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

      // Simulate tag by including "tag:" in key
      await cache.GetOrSetAsync($"tag:group1:{key1}", ct => Task.FromResult(1));
      await cache.GetOrSetAsync($"tag:group1:{key2}", ct => Task.FromResult(2));

      Assert.Contains($"tag:group1:{key1}", metricsObserver.CurrentKeys);
      Assert.Contains($"tag:group1:{key2}", metricsObserver.CurrentKeys);

      await cache.RemoveByTagAsync("group1");

      Assert.DoesNotContain($"tag:group1:{key1}", metricsObserver.CurrentKeys);
      Assert.DoesNotContain($"tag:group1:{key2}", metricsObserver.CurrentKeys);
    }
  }
}
