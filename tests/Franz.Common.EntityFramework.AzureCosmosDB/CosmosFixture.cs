using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.CosmosDb;
using Xunit;

namespace Franz.Common.AzureCosmosDB.Tests;

public sealed class CosmosFixture : IAsyncLifetime
{
  private string _endpoint = default!;

  public CosmosDbContainer Container { get; } =
      new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
          .WithPortBinding(8081, true)
          .Build();

  public async Task InitializeAsync()
  {
    await Container.StartAsync();

    var port = Container.GetMappedPublicPort(8081);
    _endpoint = $"https://localhost:{port}/";

    AppContext.SetSwitch(
      "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
      true
    );
  }

  public async Task<IsolatedCosmosContext> CreateIsolatedDatabaseContextAsync()
  {
    var services = new ServiceCollection();

    var runId = Guid.NewGuid().ToString("N");
    var databaseName = $"FranzTest_{runId}";

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Cosmos:Enabled"] = "true",
          ["Cosmos:AccountEndpoint"] = _endpoint,
          ["Cosmos:AccountKey"] = CosmosDbBuilder.DefaultAccountKey,
          ["Cosmos:DatabaseName"] = databaseName,
          ["Cosmos:ApplicationName"] = $"Franz.Tests.{runId}"
        })
        .Build();

    // Core infrastructure
    services.AddLogging();

    services.AddSingleton(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();

    services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());

    services.AddFranzCosmosDbContext<TestCosmosDbContext>(
      configuration,
      validateOnStart: false
    );

    services.AddCosmosInfrastructure<TestCosmosDbContext>(configuration);

    var provider = services.BuildServiceProvider();

    // Ensure schema exists BEFORE test runs
    using (var scope = provider.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      await db.Database.EnsureCreatedAsync();
    }

    return new IsolatedCosmosContext(provider);
  }

  public async Task DisposeAsync()
  {
    await Container.DisposeAsync();
  }
}