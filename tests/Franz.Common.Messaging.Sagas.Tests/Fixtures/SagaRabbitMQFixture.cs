#nullable enable

using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Hosting.RabbitMQ;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Fixtures;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRabbitMQFixture : IAsyncDisposable
{
  private readonly IHost _host;

  public InMemorySagaStateStore StateStore { get; }
  public ISagaStateSerializer SagaStateSerializer { get; }
  public IMessageSerializer MessageSerializer { get; }

  public IServiceProvider Services => _host.Services;

  public SagaRabbitMQFixture()
  {
    // ---------------------------------------------
    // Testcontainers: start RabbitMQ
    // ---------------------------------------------
    var rabbitFixture = new RabbitMqContainerFixture();
    rabbitFixture.InitializeAsync().GetAwaiter().GetResult();

    // Build configuration block EXACTLY as in RabbitMQ tests
    IConfiguration rabbitConfig = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:HostName"] = rabbitFixture.Host,
        ["Messaging:Port"] = rabbitFixture.Port.ToString()
      })
      .Build();

    // ---------------------------------------------
    // Saga state infrastructure
    // ---------------------------------------------
    StateStore = new InMemorySagaStateStore();
    SagaStateSerializer = new JsonSagaStateSerializer();
    MessageSerializer = new JsonMessageSerializer();

    var sagaRepository = new InMemorySagaRepository(StateStore, SagaStateSerializer);
    var sagaPipeline = new SagaExecutionPipeline();

    // ---------------------------------------------
    // Host + DI
    // ---------------------------------------------
    _host = Host.CreateDefaultBuilder()
    .ConfigureLogging(lb =>
    {
      lb.ClearProviders();
      lb.AddConsole();
      lb.AddDebug();
    })
    .ConfigureServices(services =>
    {
      // -------------------------------
      // Core serialization
      // -------------------------------
      services.AddMessagingSerialization();
      services.AddSingleton<IMessageSerializer>(MessageSerializer);
      services.AddSingleton<ISagaStateSerializer>(SagaStateSerializer);

      // -------------------------------
      // RabbitMQ Messaging Stack
      // -------------------------------
      services.AddRabbitMQMessaging(rabbitConfig);               // transport wiring
      services.AddRabbitMQMessagingConfiguration(rabbitConfig); // retries, naming, QoS

      // -------------------------------
      // Mediator (dispatcher pipeline)
      // -------------------------------
      services.AddFranzMediator(new[]
          {
                typeof(StartEvent).Assembly, // Register handlers from saga/test assemblies
                typeof(TestSaga).Assembly
        });

      // -------------------------------
      // Outbox configuration
      // -------------------------------
      services.Configure<OutboxOptions>(opts =>
      {
        opts.Enabled = true;
        opts.DeadLetterEnabled = true;
        opts.PollingInterval = TimeSpan.FromMilliseconds(200);
        opts.MaxRetries = 3;
      });

      // -------------------------------
      // Saga system
      // -------------------------------
      services.AddSingleton(TestSaga.Create);  // factory
      services.AddSingleton(StateStore);

      services.AddSingleton(provider =>
      {
        // Inner saga provider only aware of saga types
        var sagaServices = new ServiceCollection();
        sagaServices.AddSingleton(TestSaga.Create);

        var sagaProvider = sagaServices.BuildServiceProvider();

        // Router discovers sagas
        var router = new SagaRouter(sagaProvider);
        router.RegisterSaga(typeof(TestSaga));

        return new SagaOrchestrator(
                router,
                sagaRepository,
                sagaPipeline,
                publisher: provider.GetRequiredService<IMessagingPublisher>(),
                sagaProvider);
      });

      // -------------------------------
      // Hosted listener for RabbitMQ
      // -------------------------------
      services.AddRabbitMQHostedListener(opts =>
      {
        opts.HostName = rabbitFixture.Host;
        opts.Port = rabbitFixture.Port;
      });
    })
    .Build();
  }

  public async Task StartAsync() => await _host.StartAsync();

  public async Task StopAsync() => await _host.StopAsync();

  public async ValueTask DisposeAsync()
  {
    try { await _host.StopAsync(); }
    finally { _host.Dispose(); }
  }
}
