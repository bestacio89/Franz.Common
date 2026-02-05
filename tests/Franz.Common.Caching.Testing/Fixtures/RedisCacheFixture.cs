using System;
using System.Threading.Tasks;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Observability.Observers;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Franz.Common.Caching.Testing.Fixtures;

public sealed class RedisCacheFixture : IAsyncLifetime
{
  private readonly RedisContainer _container;

  public string ConnectionString => _container.GetConnectionString();

  // Shared ServiceProvider for all tests
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

    // Optional warm-up: ensure Redis is ready
    await Task.Delay(500);

    var services = new ServiceCollection();

    // Observers must be singletons to track hits/removals
    services.AddSingleton<MetricsCacheObserver>();
    services.AddSingleton<LoggingMetricsObserver>();

    // Cache registration
    services.AddFranzRedisCaching(ConnectionString)
            .AddLogging()
            .AddObservableCaching(); // Decorator last

    ServiceProvider = services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    if (ServiceProvider != null)
      await ServiceProvider.DisposeAsync();

    await _container.DisposeAsync();
  }
}
