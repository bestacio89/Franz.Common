using FluentAssertions;
// Assuming this contains AddObservableCaching
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions; // Assuming this contains your AddFranzMemoryCaching
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace Franz.Common.Caching.Tests;

public sealed class ObserverDebuggingTests
{
  [Fact]
  public void VerifyObserversAreRegistered_Should_Decorate_And_Resolve_Observers()
  {
    // Arrange
    var services = new ServiceCollection();

    // 1. Required Infrastructure (Options + Logging)
    services.AddLogging();

    // Ensure CacheOptions are registered (required by MemoryCacheProvider)
    services.AddOptions<CacheOptions>().Configure(options => {
      options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(10);
    });

    // 2. Register the Base Provider using our clean extension
    // This handles services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>()
    services.AddFranzMemoryCaching();

    // 3. Register Observers using the strategy-based extension
    // This handles services.TryAddEnumerable(...)
    services.AddCacheObserver<MetricsCacheObserver>();
    services.AddCacheObserver<LoggingMetricsObserver>();

    // 4. Apply the Scrutor Decorator
    // This wraps the registered ICacheProvider with ObservableCacheProvider
    services.AddObservableCaching();

    var sp = services.BuildServiceProvider();

    // Act
    var cacheProvider = sp.GetRequiredService<ICacheProvider>();
    var metricsObserver = sp.GetServices<ICacheObserver>().OfType<MetricsCacheObserver>().FirstOrDefault();
    var allObservers = sp.GetServices<ICacheObserver>().ToList();

    // Assert
    // The provider should now be the ObservableCacheProvider wrapper
    cacheProvider.GetType().Name.Should().Be("ObservableCacheProvider");

    metricsObserver.Should().NotBeNull();
    allObservers.Should().HaveCountGreaterThanOrEqualTo(2);

    // Debug Output
    foreach (var obs in allObservers)
    {
      // Verify specific observer types are resolved
      obs.GetType().Name.Should().MatchRegex(".*Observer");
    }
  }
}