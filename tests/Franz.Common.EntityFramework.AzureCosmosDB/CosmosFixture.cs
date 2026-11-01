using DotNet.Testcontainers.Builders;
using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.Azure.Cosmos;
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
      new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator@sha256:9a4f69121f20c1d6502db84625c71ebb441ab61b01880a7b2d3d8031c6dac429")
          .WithPortBinding(8081, true)
          .WithWaitStrategy(Wait.ForUnixContainer()
              .UntilInternalTcpPortIsAvailable(8081))
          .WithStartupTimeout(TimeSpan.FromMinutes(5))
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
    services.AddScoped<ITenantExecutionContextAccessor, CosmosTestTenantExecutionContextAccessor>();

    services.AddSingleton(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();

    services.AddSingleton<IDispatcher>(Substitute.For<IDispatcher>());

    services.AddFranzCosmosDbContext<TestCosmosDbContext>(
      configuration,
      validateOnStart: false
    );

    services.AddCosmosInfrastructure<TestCosmosDbContext>(configuration);
    services.AddCosmosTenantRepository<CosmosSdkTenantDocument>("TenantDocuments");

    var provider = services.BuildServiceProvider();

    // Ensure schema exists BEFORE test runs
    using (var scope = provider.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
      await db.Database.EnsureCreatedAsync();
      var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
      await cosmosClient.GetDatabase(databaseName).CreateContainerIfNotExistsAsync(
        new ContainerProperties("TenantDocuments", "/tenantId"),
        throughput: 400);
    }

    return new IsolatedCosmosContext(provider);
  }

  public async Task DisposeAsync()
  {
    await Container.DisposeAsync();
  }
}
