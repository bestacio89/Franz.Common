using Franz.Common.Caching.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Franz.Common.Caching.Testing.Fixtures;
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

    // Register cache + observers in correct order
    services.AddFranzRedisCaching(ConnectionString)
            .AddMetricsCacheObserver()
            .AddLoggingMetricsCacheObserver()
            .AddLogging()
            .AddObservableCaching();

    ServiceProvider = services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    if (ServiceProvider != null)
      await ServiceProvider.DisposeAsync();

    await _container.DisposeAsync();
  }
}
