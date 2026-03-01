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

   
}