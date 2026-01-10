#nullable enable
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Fixtures;
using Franz.Common.Messaging.Sagas.Handlers;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRabbitMQFixture : IAsyncLifetime
{
  public IHost Host { get; private set; } = default!;
  public IServiceProvider Services => Host.Services;

  public InMemorySagaStateStore StateStore { get; } = new();
  public JsonSagaStateSerializer Serializer { get; } = new();

  public async Task InitializeAsync()
  {
    // -------------------------------
    // Start RabbitMQ container
    // -------------------------------
    var rabbit = new RabbitMqContainerFixture();
    await rabbit.InitializeAsync();

    // -------------------------------
    // Messaging configuration
    // -------------------------------
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:HostName"] = rabbit.Host,
          ["Messaging:Port"] = rabbit.Port.ToString(),
          ["Messaging:ServiceName"] = "testsagas"   // 🔥 required for topology matching
        })
        .Build();

    // -------------------------------
    // Host builder
    // -------------------------------
    Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
          // -------------------
          // Saga infrastructure
          // -------------------
          services.AddSingleton(StateStore);
          services.AddSingleton<ISagaRepository>(
                  sp => new InMemorySagaRepository(StateStore, Serializer));

          services.AddFranzSagas(opts => opts.ValidateMappings = false);
          services.AddFranzMediator(new[] { typeof(StartEvent).Assembly });

          services.AddTransient<TestSaga>();
          services.AddTransient<SagaExecutionPipeline>();
          services.AddTransient<SagaOrchestrator>();

          // -------------------
          // Messaging
          // -------------------
          services.RemoveAll<IMessageHandler>();
          services.AddSingleton<IMessageHandler, SagaDispatchingMessageHandler>();

          services.AddMessagingSerialization();
          services.AddRabbitMQMessaging(config);

          // This starts the Listener + provisions queue using DEFAULT topology
          services.AddRabbitMQHostedListener(o =>
          {
            o.HostName = rabbit.Host;
            o.Port = rabbit.Port;
          });
        })
        .Build();

    await Host.StartAsync();
  }

  public async Task DisposeAsync()
  {
    await Host.StopAsync();
    Host.Dispose();
  }
}
