#nullable enable

using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Fixtures;
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

  public MongoContainerFixture Mongo { get; } = new();
  public RabbitMqContainerFixture Rabbit { get; } = new();

  public JsonSagaStateSerializer Serializer { get; } = new();

  public async Task InitializeAsync()
  {
    await Rabbit.InitializeAsync();
    await Mongo.InitializeAsync();

    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:HostName"] = Rabbit.Host,
          ["Messaging:Port"] = Rabbit.Port.ToString(),
          ["Messaging:ServiceName"] = "testsagas"
        })
        .Build();

    Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
          // -------------------------
          // Saga registration
          // -------------------------
          var builder = services.AddFranzSagas(opts =>
          {
            opts.ValidateMappings = false;
          });

          builder.AddSaga<TestSaga>();

          services.AddTransient<TestSaga>();

          // -------------------------
          // Saga repository (Mongo)
          // -------------------------
          services.AddSingleton<ISagaRepository>(sp =>
          {
            var client = new MongoClient(Mongo.ConnectionString);
            var database = client.GetDatabase(Mongo.DatabaseName);

            return new MongoSagaRepository(database, Serializer);
          });

          // Messaging + mediator
          services.AddFranzMediator(new[] { typeof(StartEvent).Assembly });
          services.AddMessagingSerialization();
          services.AddRabbitMQMessaging(config);

          services.AddRabbitMQHostedListener(o =>
          {
            o.HostName = Rabbit.Host;
            o.Port = Rabbit.Port;
          });
        })
        .Build();

    // Host MUST start before building sagas
    Host.Services.BuildFranzSagas();
    await Host.StartAsync();
    
  }

  public async Task DisposeAsync()
  {
    await Host.StopAsync();
    Host.Dispose();
    await Rabbit.DisposeAsync();
    await Mongo.DisposeAsync();
  }
}
