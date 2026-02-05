using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability.Observers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Caching.Testing.Tests
{
  public class ObserverDebuggingTests
  {
    [Fact]
    public async Task VerifyObserversAreRegistered()
    {
      // Arrange
      var services = new ServiceCollection();

      services.AddFranzMemoryCaching()
              .AddLogging()
              .AddObservableCaching()
              .AddMetricsCacheObserver();

      var sp = services.BuildServiceProvider();

      // Act - Get all registrations
      var cacheProvider = sp.GetRequiredService<ICacheProvider>();
      var metricsObserver = sp.GetRequiredService<MetricsCacheObserver>();
      var allObservers = sp.GetServices<Franz.Common.Caching.Observability.ICacheObserver>();

      // Assert
      Assert.NotNull(cacheProvider);
      Assert.NotNull(metricsObserver);
      Assert.NotEmpty(allObservers);

      Console.WriteLine($"Cache Provider Type: {cacheProvider.GetType().Name}");
      Console.WriteLine($"Observers Count: {System.Linq.Enumerable.Count(allObservers)}");
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
              .AddObservableCaching()
              .AddMetricsCacheObserver();

      var sp = services.BuildServiceProvider();
      var cache = sp.GetRequiredService<ICacheProvider>();
      var observer = sp.GetRequiredService<MetricsCacheObserver>();

      string key = $"test_{Guid.NewGuid()}";

      // Verify initial state
      Assert.Empty(observer.CurrentKeys);
      Assert.Equal(0, observer.TotalSets);

      // Act
      await cache.GetOrSetAsync(key, ct => Task.FromResult(123));

      // Assert
      Console.WriteLine($"TotalSets: {observer.TotalSets}");
      Console.WriteLine($"CurrentKeys Count: {observer.CurrentKeys.Count}");
      Console.WriteLine($"CurrentKeys: {string.Join(", ", observer.CurrentKeys)}");

      Assert.Equal(1, observer.TotalSets);
      Assert.Contains(key, observer.CurrentKeys);
    }
  }
}