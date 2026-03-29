#nullable enable
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;

public sealed class RabbitMQHostingFixture : IAsyncLifetime
{
  private readonly RabbitMqContainerFixture _rabbit;
  private readonly MongoContainerFixture _mongo;

  public IHost? Host { get; private set; }

  public IServiceProvider Services =>
      Host?.Services ?? throw new InvalidOperationException("Host not started. Call InitializeAsync first.");

  public RabbitMQHostingFixture()
  {
    _rabbit = new RabbitMqContainerFixture();
    _mongo = new MongoContainerFixture();
  }

  public async Task InitializeAsync()
  {
    // 1️⃣ Start containers in parallel
    // Senior Note: Parallel startup minimizes cold-start latency for the integration suite.
    await Task.WhenAll(
        _rabbit.InitializeAsync(),
        _mongo.InitializeAsync());

    // 2️⃣ Build host with merged configuration
    Host = BuildHost();
    await Host.StartAsync();
  }

  public async Task DisposeAsync()
  {
    // 3️⃣ Graceful shutdown of the Host before killing infrastructure
    if (Host != null)
    {
      await Host.StopAsync();
      Host.Dispose();
    }

    await Task.WhenAll(
        _rabbit.DisposeAsync(),
        _mongo.DisposeAsync());
  }

  private IHost BuildHost()
  {
    // Consolidate configurations from all fixtures
    var configDict = new Dictionary<string, string?>();

    // Merge RabbitMQ URI-based configuration
    foreach (var (key, value) in _rabbit.GetConfiguration())
    {
      configDict[key] = value;
    }

    // Merge Mongo configuration
    foreach (var (key, value) in _mongo.GetConfiguration())
    {
      configDict[key] = value;
    }

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(configDict)
        .Build();

    return new HostBuilder()
        .ConfigureServices((hostContext, services) =>
        {
          services.AddLogging();
          services.AddDefaultMessageSerializer();

          // 🔹 Mediator: Scan assembly for Handlers
          services.AddFranzMediator(new[] { typeof(RabbitMQHostingFixture).Assembly });

          // 🔹 Full RabbitMQ Messaging infra 
          // Senior Note: AddRabbitMQMessaging will resolve the 'BootStrapServers' key
          // from the configuration provided above.
          services.AddRabbitMQMessaging(configuration);
          services.AddSingleton<IConnectionFactory, ConnectionFactory>();
          // 🔹 Mongo-backed message store (for Outbox / replay)
          // Senior Note: Utilizing ConnectionString and DatabaseName from the fixture
          services.AddMongoMessageStore(_mongo.ConnectionString, _mongo.DatabaseName);

          // 🔹 Hosted listeners (Consumer Services)
          // These will use the URI-based connection factory resolved via DI
          services.AddRabbitMQHostedListener(_ => { });
          services.AddOutboxHostedListener(_ => { });
        })
        .Build();
  }
}