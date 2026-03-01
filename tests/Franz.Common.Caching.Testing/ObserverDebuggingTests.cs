using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Tests
{
  public class ObserverDebuggingTests
  {
    [Fact]
    public void VerifyObserversAreRegistered()
    {
      // Arrange
      var services = new ServiceCollection();

      // Ensure the memory cache provider is registered first
      services.AddFranzMemoryCaching(); // registers ICacheProvider -> MemoryCacheProvider
      services.AddLogging();
      services.AddMetricsCacheObserver();
      services.AddObservableCaching(); // now it can resolve the MemoryCacheProvider

      var sp = services.BuildServiceProvider();

      // Act
      var cacheProvider = sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = sp.GetRequiredService<MetricsCacheObserver>();
      var allObservers = sp.GetServices<Franz.Common.Caching.Observability.ICacheObserver>();

      // Assert
      Assert.NotNull(cacheProvider);
      Assert.NotNull(metricsObserver);
      Assert.NotEmpty(allObservers);

      Console.WriteLine($"Cache Provider Type: {cacheProvider.GetType().Name}");
      Console.WriteLine($"Observers Count: {allObservers.Count()}");
      foreach (var obs in allObservers)
      {
        Console.WriteLine($"  - Observer: {obs.GetType().Name}");
      }
    }

    [Fact]
    public async Task VerifyObserverIsCalledOnSet()
    {
      // Arrange
      var services = new ServiceCollection();

      services.AddFranzMemoryCaching()
              .AddLogging()
              .AddMetricsCacheObserver()      // ← Register observers FIRST
              .AddObservableCaching();         // ← Then add decoration

      var sp = services.BuildServiceProvider();
      var cache = sp.GetRequiredService<ICacheProvider>();
      var observer = sp.GetRequiredService<MetricsCacheObserver>();
      var allObservers = sp.GetServices<Franz.Common.Caching.Observability.ICacheObserver>();

      string key = $"test_{Guid.NewGuid()}";

      // Debug output
      Console.WriteLine($"Cache Provider Type: {cache.GetType().Name}");
      Console.WriteLine($"Observers registered: {System.Linq.Enumerable.Count(allObservers)}");

      if (cache is ObservableCacheProvider observable)
      {
        Console.WriteLine("✓ Cache is wrapped with ObservableCacheProvider");
        // Use reflection to check if observers are injected
        var field = typeof(ObservableCacheProvider).GetField("_observers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
          var observers = field.GetValue(observable) as Array;
          Console.WriteLine($"✓ Observers in decorator: {observers?.Length ?? 0}");
        }
      }
      else
      {
        Console.WriteLine($"✗ Cache is NOT wrapped! Type: {cache.GetType().FullName}");
      }

      // Verify initial state
      Assert.Empty(observer.CurrentKeys);
      Assert.Equal(0, observer.TotalSets);

      // Act
      await cache.GetOrSetAsync(key, ct => Task.FromResult(123));

      // Assert
      Console.WriteLine($"TotalSets after operation: {observer.TotalSets}");
      Console.WriteLine($"CurrentKeys Count: {observer.CurrentKeys.Count}");
      Console.WriteLine($"CurrentKeys: {string.Join(", ", observer.CurrentKeys)}");

      Assert.Equal(1, observer.TotalSets);
      Assert.Contains(key, observer.CurrentKeys);
    }
  }
}