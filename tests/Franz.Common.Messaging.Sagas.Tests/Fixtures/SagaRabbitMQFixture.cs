#nullable enable
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Connections;

using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Fixtures;
using Franz.Common.Messaging.Sagas.Handlers;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Mongo;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;

using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Sagas;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRabbitMQMongoFixture : IAsyncLifetime
{
  public IHost Host { get; private set; } = default!;
  public IServiceProvider Services => Host.Services;

  public RabbitMqContainerFixture Rabbit { get; } = new();
  public MongoContainerFixture Mongo { get; } = new();
  public JsonSagaStateSerializer Serializer { get; } = new();

  public async Task InitializeAsync()
  {
    // -------------------------------
    // Start Test Containers
    // -------------------------------
    await Rabbit.InitializeAsync();
    await Mongo.InitializeAsync();

    // -------------------------------
    // Messaging Config
    // -------------------------------
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:HostName"] = Rabbit.Host,
          ["Messaging:Port"] = Rabbit.Port.ToString(),
          ["Messaging:ServiceName"] = "testsagas"
        })
        .Build();

    // -------------------------------
    // Build Host
    // -------------------------------
    Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        // ======================================
        // 1) MONGO SAGA PERSISTENCE (Real Infra)
        // ======================================
        var client = new MongoClient(Mongo.ConnectionString);
        var db = client.GetDatabase(Mongo.DatabaseName);

        services.AddSingleton<ISagaRepository>(
          sp => new MongoSagaRepository(db, Serializer));

        // ======================================
        // 2) Franz Saga Infrastructure
        // ======================================
        services.AddFranzSagas(opts => opts.ValidateMappings = false);
        services.AddFranzMediator(new[] { typeof(StartEvent).Assembly });

        services.AddTransient<TestSaga>();
        services.AddTransient<SagaExecutionPipeline>();
        services.AddTransient<SagaOrchestrator>();

        // ======================================
        // 3) Messaging Pipeline
        // ======================================
        // DO NOT REMOVE other handlers — add the saga dispatcher
        services.AddNoDuplicateScoped<IMessageHandler, SagaDispatchingMessageHandler>();

        services.AddMessagingSerialization();

        services.AddRabbitMQMessaging(config);

        services.AddRabbitMQHostedListener(o =>
        {
          o.HostName = Rabbit.Host;
          o.Port = Rabbit.Port;
        });
      })
      .Build();

    // -------------------------------
    // START HOST THEN ACTIVATE SAGAS
    // -------------------------------
    await Host.StartAsync();

    // 🔥 REQUIRED: Build routing + finalize saga registration
    Host.Services.BuildFranzSagas();
  }

  public async Task DisposeAsync()
  {
    await Host.StopAsync();
    Host.Dispose();

    await Mongo.DisposeAsync();
    await Rabbit.DisposeAsync();
  }
}
