using System.Collections.Concurrent;
using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.CosmosDb;
using Xunit;

namespace Franz.Common.AzureCosmosDB.Tests;

public sealed class CosmosFixture : IAsyncLifetime
{
  private string _endpoint = default!;
  private readonly ConcurrentBag<IServiceProvider> _allocatedProviders = new();

  public CosmosDbContainer Container { get; } =
      new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
          .WithPortBinding(8081, true)
          .Build();

  public async Task InitializeAsync()
  {
    await Container.StartAsync();

    var port = Container.GetMappedPublicPort(8081);
    _endpoint = $"https://localhost:{port}/";

    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
  }

  /// <summary>
  /// Builds a unique, ephemeral logical database context bound to the shared emulator container.
  /// </summary>
  public async Task<IsolatedCosmosContext> CreateIsolatedDatabaseContextAsync()
  {
    var services = new ServiceCollection();
    var databaseName = $"FranzTest_{Guid.NewGuid():N}";

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Cosmos:Enabled"] = "true",
          ["Cosmos:AccountEndpoint"] = _endpoint,
          ["Cosmos:AccountKey"] = CosmosDbBuilder.DefaultAccountKey,
          ["Cosmos:DatabaseName"] = databaseName,
          ["Cosmos:ApplicationName"] = $"Franz.Tests.Cosmos.{databaseName}"
        })
        .Build();

    services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());
    services.AddLogging();
    services.AddSingleton(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();

    services.AddFranzCosmosDbContext<TestCosmosDbContext>(configuration, validateOnStart: false);
    services.AddCosmosInfrastructure<TestCosmosDbContext>(configuration);

    var provider = services.BuildServiceProvider();
    _allocatedProviders.Add(provider);

    // Provision the schema using an isolated database admin instance
    using (var initScope = provider.CreateScope())
    {
      var initDb = initScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      await initDb.Database.EnsureCreatedAsync();
    }

    return new IsolatedCosmosContext(provider);
  }

  public async Task DisposeAsync()
  {
    foreach (var provider in _allocatedProviders)
    {
      if (provider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    await Container.DisposeAsync();
  }
}

/// <summary>
/// Encapsulates the runtime scope boundaries of an individual execution thread.
/// </summary>
public sealed class IsolatedCosmosContext
{
  private readonly IServiceProvider _provider;

  public IsolatedCosmosContext(IServiceProvider provider)
  {
    _provider = provider;
  }

  public IServiceScope CreateScope() => _provider.CreateScope();

  public async Task CleanUpAsync()
  {
    // Resolve a clean, standalone instance solely tasked with tearing down this isolated database topology
    using var cleanupScope = _provider.CreateScope();
    var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
    await cleanupDb.Database.EnsureDeletedAsync();
  }
}