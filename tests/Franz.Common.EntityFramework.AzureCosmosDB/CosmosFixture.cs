using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.AzureCosmosDB.Tests;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.CosmosDb;

public sealed class CosmosFixture : IAsyncLifetime
{
  private IServiceProvider _provider = default!;
  private TestCosmosDbContext _db = default!;

  public CosmosDbContainer Container { get; } =
      new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
          .WithPortBinding(8081, true)
          .Build();

  public async Task InitializeAsync()
  {
    await Container.StartAsync();

    var services = new ServiceCollection();

    var databaseName = $"FranzTest_{Guid.NewGuid():N}";
    var port = Container.GetMappedPublicPort(8081);
    var endpoint = $"https://localhost:{port}/";

    var configuration =
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
              ["Cosmos:Enabled"] = "true",
              ["Cosmos:AccountEndpoint"] = endpoint,
              ["Cosmos:AccountKey"] = CosmosDbBuilder.DefaultAccountKey,
              ["Cosmos:DatabaseName"] = databaseName,
              ["Cosmos:ApplicationName"] = "Franz.Tests.Cosmos"
            })
            .Build();

    services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());

    services.AddLogging();

    AppContext.SetSwitch(
        "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
        true);

    // IMPORTANT: factories should behave consistently
    services.AddSingleton(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();

    services.AddFranzCosmosDbContext<TestCosmosDbContext>(
        configuration,
        validateOnStart: false);

    services.AddCosmosInfrastructure<TestCosmosDbContext>(configuration);

    _provider = services.BuildServiceProvider();

    // 🔥 SINGLE INITIALIZATION POINT
    using var scope = _provider.CreateScope();

    _db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    await _db.Database.EnsureCreatedAsync();
  }

  public IServiceScope CreateScope() => _provider.CreateScope();

  public TestCosmosDbContext CreateDb() => _provider.GetRequiredService<TestCosmosDbContext>();

  public async Task DisposeAsync()
  {
    if (_provider is IDisposable d)
      d.Dispose();

    await Container.DisposeAsync();
  }
}


[CollectionDefinition("Cosmos", DisableParallelization = true)]
public class CosmosCollection { }