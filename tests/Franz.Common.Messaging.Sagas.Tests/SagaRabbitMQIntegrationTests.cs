#nullable enable

using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Fixtures;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Fixtures;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Messaging.Sagas.Tests.Integration;

public sealed class SagaRabbitMqIntegrationTests :
  IClassFixture<RabbitMqContainerFixture>,
  IClassFixture<MongoContainerFixture>
{
  private readonly RabbitMqContainerFixture _rabbit;
  private readonly MongoContainerFixture _mongo;

  public SagaRabbitMqIntegrationTests(
    RabbitMqContainerFixture rabbit,
    MongoContainerFixture mongo)
  {
    _rabbit = rabbit;
    _mongo = mongo;
  }

  private IConfiguration BuildRabbitConfiguration()
    => new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:HostName"] = _rabbit.Host,
        ["Messaging:Port"] = _rabbit.Port.ToString()
      })
      .Build();

  [Fact]
  public async Task Saga_executes_end_to_end_inside_real_rabbitmq_host()
  {
    var configuration = BuildRabbitConfiguration();

    using var host = Host.CreateDefaultBuilder()

      // 🔧 FIX #1: Do NOT crash host on background service shutdown
      .ConfigureHostOptions(options =>
      {
        options.BackgroundServiceExceptionBehavior =
          BackgroundServiceExceptionBehavior.Ignore;
      })

      .ConfigureServices(services =>
      {
        services.AddLogging();

        // =========================
        // Messaging infrastructure
        // =========================
        services.AddMessagingSerialization();
        services.AddRabbitMQMessaging(configuration);
        services.AddMongoMessageStore(
          connectionString: _mongo.ConnectionString,
          dbName: _mongo.DatabaseName);

        // =========================
        // Mediator
        // =========================
        services.AddFranzMediator(new[]
        {
          typeof(StartEvent).Assembly
        });

        // =========================
        // Saga persistence (INTENTIONAL: in-memory)
        // =========================
        services.AddSingleton<InMemorySagaStateStore>();
        services.AddSingleton<ISagaStateSerializer, JsonSagaStateSerializer>();
        services.AddSingleton<ISagaRepository, InMemorySagaRepository>();

        // =========================
        // Saga registration
        // =========================
        services.AddFranzSagas();

        // =========================
        // Hosting
        // =========================
        services.AddRabbitMQHostedListener(_ => { });
        services.AddOutboxHostedListener(opts =>
        {
          opts.PollingInterval = TimeSpan.FromMilliseconds(100);
        });
      })
      .Build();

    // 🔑 Finalize saga registration
    host.Services.BuildFranzSagas();

    await host.StartAsync();

    // =========================
    // ACT
    // =========================
    var mediator = host.Services.GetRequiredService<IDispatcher>();

    await mediator.PublishNotificationAsync(new StartEvent("saga-1"));
    await mediator.PublishNotificationAsync(new StepEvent("saga-1"));

    // =========================
    // ASSERT (eventual consistency)
    // =========================
    var store = host.Services.GetRequiredService<InMemorySagaStateStore>();
    var serializer = host.Services.GetRequiredService<ISagaStateSerializer>();

    TestSagaState? state = null;
    var timeout = TimeSpan.FromSeconds(5);
    var start = DateTime.UtcNow;

    // 🔧 FIX #2: wait until saga executes
    while (DateTime.UtcNow - start < timeout)
    {
      if (store.Store.TryGetValue("saga-1", out var json))
      {
        state = (TestSagaState)
          serializer.Deserialize(json!, typeof(TestSagaState));
        break;
      }

      await Task.Delay(100);
    }

    Assert.NotNull(state);
    Assert.Equal("saga-1", state!.Id);
    Assert.Equal(2, state.Counter);

    await host.StopAsync();
  }
}
