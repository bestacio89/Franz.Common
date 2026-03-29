using Franz.Common.AzureCosmosDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Franz.Common.Caching.Tests.Fixtures;

public sealed class RedisCacheFixture : IAsyncLifetime
{
  private readonly RedisContainer _container;

  // 🛠️ This must be public so the Tests can see it
  public string ConnectionString => _container.GetConnectionString();

  public ServiceProvider ServiceProvider { get; private set; } = null!;

  public RedisCacheFixture()
  {
    _container = new RedisBuilder("redis:7.2-alpine")
        .WithCleanUp(true)
        .Build();
  }

  public async Task InitializeAsync()
  {
    await _container.StartAsync();

    var services = new ServiceCollection();
    services.AddLogging();

    // We register the multiplexer here so tests can resolve it
    services.AddFranzRedisCaching(options =>
    {
      options.ConnectionString = ConnectionString;
      options.KeyPrefix = "test:";
    });

    ServiceProvider = services.BuildServiceProvider();
  }

  public async Task DisposeAsync()
  {
    if (ServiceProvider != null)
      await ServiceProvider.DisposeAsync();

    await _container.DisposeAsync();
  }
}