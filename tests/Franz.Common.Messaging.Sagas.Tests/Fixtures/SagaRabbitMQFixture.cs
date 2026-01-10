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
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRabbitMQFixture : IAsyncLifetime
{
  public IHost Host { get; private set; } = default!;
  public IServiceProvider Services => Host.Services;

  public InMemorySagaStateStore StateStore { get; } = new();
  public JsonSagaStateSerializer Serializer { get; } = new();

  public string QueueName { get; private set; } = default!;
  public string ExchangeName { get; private set; } = default!;

  public async Task InitializeAsync()
  {
    // =============================
    // RabbitMQ container fixture
    // =============================
    var rabbit = new RabbitMqContainerFixture();
    await rabbit.InitializeAsync();

    // =============================
    // Generate deterministic names
    // =============================
    QueueName = QueueNamer.GetQueueName(typeof(TestSaga).Assembly);
    ExchangeName = ExchangeNamer.GetEventExchangeName(typeof(TestSaga).Assembly);

    // =============================
    // Build configuration
    // =============================
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:HostName"] = rabbit.Host,
          ["Messaging:Port"] = rabbit.Port.ToString(),
          ["Messaging:ServiceName"] = "testsagas"     // 🔥 IMPORTANT
        })
        .Build();

    // =============================
    // Host
    // =============================
    Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
          // Saga runtime components
          services.AddSingleton(StateStore);
          services.AddSingleton<ISagaRepository>(sp =>
                  new InMemorySagaRepository(StateStore, Serializer));

          services.AddFranzSagas(opts => opts.ValidateMappings = false);
          services.AddFranzMediator(new[] { typeof(StartEvent).Assembly });

          // Register saga
          services.AddTransient<TestSaga>();
          services.AddTransient<SagaExecutionPipeline>();
          services.AddTransient<SagaOrchestrator>();

          // Register messaging
          services.RemoveAll<IMessageHandler>();  // remove any default handlers

          services.AddSingleton<IAssemblyAccessor>(
              new TestAssemblyAccessor(new AssemblyWrapper(typeof(TestSaga).Assembly))); // ensures queue name alignment

          services.AddSingleton<IMessageHandler, SagaDispatchingMessageHandler>();

          services.AddMessagingSerialization();
          services.AddRabbitMQMessaging(config);
          services.AddRabbitMQHostedListener(o =>
          {
            o.HostName = rabbit.Host;
            o.Port = rabbit.Port;
          });
        })
        .Build();

    await Host.StartAsync();

    // =============================
    // Declare queue & exchange
    // =============================
    var connProvider = Services.GetRequiredService<IConnectionProvider>();
    using var connection = connProvider.Current;
    using var channel = await connection.CreateChannelAsync();


    await channel.ExchangeDeclareAsync(
        exchange: ExchangeName,
        type: ExchangeType.Fanout,
        durable: true,
        autoDelete: false,
        arguments: null);

    await channel.QueueDeclareAsync(
        queue: QueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    await channel.QueueBindAsync(
        queue: QueueName,
        exchange: ExchangeName,
        routingKey: "",
        arguments: null);
  }

  public async Task DisposeAsync()
  {
    await Host.StopAsync();
    Host.Dispose();
  }
}
