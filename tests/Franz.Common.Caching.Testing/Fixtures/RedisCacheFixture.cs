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

    // Build shared DI container
    var services = new ServiceCollection();

    services.AddFranzRedisCaching(ConnectionString)
            .AddLogging()
            .AddMetricsCacheObserver()       // Register observers BEFORE decorator
            .AddLoggingMetricsCacheObserver()
            .AddObservableCaching();         // Decorator last

    ServiceProvider = services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    if (ServiceProvider != null)
      await ServiceProvider.DisposeAsync();

    await _container.DisposeAsync();
  }
}
