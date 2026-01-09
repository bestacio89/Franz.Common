#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Fixtures;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

/// <summary>
/// Full end-to-end saga fixture using:
///  - Testcontainers RabbitMQ
///  - Real Saga orchestrator
///  - In-memory saga state persistence
///  - Real messaging & outbox pipeline (you fill RabbitMQ registration)
/// </summary>
public sealed class SagaRabbitMQFixture : IAsyncDisposable
{
  private readonly IHost _host;

  public InMemorySagaStateStore StateStore { get; }
  public IMessageSerializer MessageSerializer { get; }
  public ISagaStateSerializer SagaStateSerializer { get; }

  public IServiceProvider Services => _host.Services;

  public SagaRabbitMQFixture()
  {
    // -----------------------------------
    // Start RabbitMQ test container
    // -----------------------------------
    var rabbitFixture = new RabbitMqContainerFixture();
    rabbitFixture.InitializeAsync().GetAwaiter().GetResult();

    var rabbitHost = rabbitFixture.Host;
    var rabbitPort = rabbitFixture.Port;

    // -----------------------------------
    // Saga State + Serializer
    // -----------------------------------
    StateStore = new InMemorySagaStateStore();
    SagaStateSerializer = new JsonSagaStateSerializer();

    var sagaRepository = new InMemorySagaRepository(StateStore, SagaStateSerializer);
    var sagaPipeline = new SagaExecutionPipeline();

    // -----------------------------------
    // Serialization for messaging
    // -----------------------------------
    MessageSerializer = new JsonMessageSerializer();

    // -----------------------------------
    // Host + DI
    // -----------------------------------
    _host = Host.CreateDefaultBuilder()
        .ConfigureLogging(lb =>
        {
          lb.ClearProviders();
          lb.AddConsole();
          lb.AddDebug();
        })
        .ConfigureServices(services =>
        {
          // -----------------------------
          // Saga DI
          // -----------------------------
          services.AddSingleton(StateStore);
          services.AddSingleton<ISagaStateSerializer>(SagaStateSerializer);
          services.AddSingleton<IMessageSerializer>(MessageSerializer);

          services.AddSingleton<TestSaga>();

          services.AddSingleton(provider =>
          {
            // Inner provider that knows only saga types
            var sagaServices = new ServiceCollection();
            sagaServices.AddSingleton<TestSaga>();

            var sagaProvider = sagaServices.BuildServiceProvider();

            // Router registers all sagas
            var router = new SagaRouter(sagaProvider);
            router.RegisterSaga(typeof(TestSaga));

            return new SagaOrchestrator(
                    router,
                    sagaRepository,
                    sagaPipeline,
                    publisher: NullMessagingPublisher.Instance,
                    sagaProvider);
          });

          // -----------------------------
          // Messaging + Outbox pipeline
          // YOU MUST FILL THESE LINES
          // -----------------------------

          // Example: (use your real extension methods)
          //
          // services.AddRabbitMqMessaging(options =>
          // {
          //     options.HostName = rabbitHost;
          //     options.Port = rabbitPort;
          //     options.UserName = "guest";
          //     options.Password = "guest";
          // });
          //
          // services.AddOutboxPublisher();
          // services.Configure<OutboxOptions>(opts =>
          // {
          //     opts.Enabled = true;
          //     opts.PollingInterval = TimeSpan.FromMilliseconds(200);
          //     opts.MaxRetries = 3;
          //     opts.DeadLetterEnabled = true;
          // });
          //
          // services.AddSingleton<IQueueProvisioner, DefaultQueueProvisioner>();

        })
        .Build();
  }

  public async Task StartAsync(CancellationToken ct = default) =>
      await _host.StartAsync(ct);

  public async Task StopAsync(CancellationToken ct = default) =>
      await _host.StopAsync(ct);

  public async ValueTask DisposeAsync()
  {
    try
    {
      await _host.StopAsync();
    }
    finally
    {
      _host.Dispose();
    }
  }
}
