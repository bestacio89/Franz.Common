using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability;
using Franz.Common.Caching.Observability.Observers;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Franz.Common.Caching.Tests.Fixtures;

public sealed class RedisCacheFixture : IAsyncLifetime
{
  private readonly RedisContainer _container;
  public string ConnectionString => _container.GetConnectionString();
  public ServiceProvider ServiceProvider { get; private set; } = null!;

  public RedisCacheFixture()
  {
    _container = new RedisBuilder()
        .WithImage("redis:7.2-alpine")
        .WithCleanUp(true)
        .Build();
  }

  public async Task InitializeAsync()
  {
    await _container.StartAsync();
    await Task.Delay(500); // optional warm-up

    var services = new ServiceCollection();

    // Register logging first
    services.AddLogging();

    // Register observers as singletons BEFORE cache
    services.AddSingleton<MetricsCacheObserver>();
    services.AddSingleton<LoggingMetricsObserver>();
    services.AddSingleton<ICacheObserver>(sp => sp.GetRequiredService<MetricsCacheObserver>());
    services.AddSingleton<ICacheObserver>(sp => sp.GetRequiredService<LoggingMetricsObserver>());

    // Register Redis caching and make sure it uses observers
    services.AddFranzRedisCaching(ConnectionString);

    // Add observable caching (hooks up observers to cache)
    services.AddObservableCaching();

    ServiceProvider = services.BuildServiceProvider();

    // Ensure cache provider is initialized and ready
    var cache = ServiceProvider.GetRequiredService<ICacheProvider>();
    
  }

  public async Task DisposeAsync()
  {
    if (ServiceProvider != null)
      await ServiceProvider.DisposeAsync();

    await _container.DisposeAsync();
  }
}