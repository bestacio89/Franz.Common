using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Franz.Common.Caching.Providers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Tests
{
  public class ObserverDebuggingTests
  {
    public void VerifyObserversAreRegistered()
    {
      // Arrange
      var services = new ServiceCollection();

      // Step 1: Register MemoryCacheProvider **both as concrete and interface**
      services.AddSingleton<MemoryCacheProvider>();
      services.AddSingleton<ICacheProvider>(sp => sp.GetRequiredService<MemoryCacheProvider>());

      // Step 2: Logging and observers
      services.AddLogging();
      services.AddMetricsCacheObserver();

      // Step 3: Decorate with ObservableCacheProvider
      services.AddObservableCaching(); // now it can resolve MemoryCacheProvider

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

      // Step 1: Register the underlying MemoryCacheProvider
      services.AddFranzMemoryCaching(); // ensures MemoryCacheProvider is registered
      services.AddSingleton<MemoryCacheProvider>();
      services.AddSingleton<ICacheProvider>(sp => sp.GetRequiredService<MemoryCacheProvider>());

      // Step 2: Register observers
      services.AddMetricsCacheObserver();

      // Step 3: Decorate the cache with ObservableCacheProvider
      services.AddObservableCaching();

      // Step 4: Logging (optional but recommended)
      services.AddLogging();

      var sp = services.BuildServiceProvider();

      var cache = sp.GetRequiredService<ICacheProvider>();
      var observer = sp.GetRequiredService<MetricsCacheObserver>();
      var allObservers = sp.GetServices<Franz.Common.Caching.Observability.ICacheObserver>();

      string key = $"test_{Guid.NewGuid()}";

      // Debug output
      Console.WriteLine($"Cache Provider Type: {cache.GetType().Name}");
      Console.WriteLine($"Observers registered: {allObservers.Count()}");

      if (cache is ObservableCacheProvider observable)
      {
        Console.WriteLine("✓ Cache is wrapped with ObservableCacheProvider");
        // Reflection to check injected observers
        var field = typeof(ObservableCacheProvider).GetField("_observers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
          var observersArray = field.GetValue(observable) as Array;
          Console.WriteLine($"✓ Observers in decorator: {observersArray?.Length ?? 0}");
        }
      }
      else
      {
        Console.WriteLine($"✗ Cache is NOT wrapped! Type: {cache.GetType().FullName}");
      }

      // Verify initial state
      Assert.Empty(observer.CurrentKeys);
      Assert.Equal(0, observer.TotalSets);

      // Act: Set a value in the cache
      await cache.GetOrSetAsync(key, ct => Task.FromResult(123));

      // Assert: Observer should have tracked the set
      Console.WriteLine($"TotalSets after operation: {observer.TotalSets}");
      Console.WriteLine($"CurrentKeys Count: {observer.CurrentKeys.Count}");
      Console.WriteLine($"CurrentKeys: {string.Join(", ", observer.CurrentKeys)}");

      Assert.Equal(1, observer.TotalSets);
      Assert.Contains(key, observer.CurrentKeys);
    }
  }
}