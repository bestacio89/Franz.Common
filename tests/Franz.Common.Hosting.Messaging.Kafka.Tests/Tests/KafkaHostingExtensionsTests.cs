using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.Kafka;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.Kafka.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Franz.Common.MongoDB.Extensions;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Fixtures;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.ServiceCollections;

public sealed class KafkaHostingExtensionsTests
  : IClassFixture<KafkaContainerFixture>
{
  private readonly KafkaContainerFixture _kafka;
  private readonly MongoContainerFixture _mongo = new();
  public KafkaHostingExtensionsTests(KafkaContainerFixture kafka)
  {
    _kafka = kafka;
  }

  private IConfiguration BuildKafkaConfiguration()
  {
    return new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Messaging:BootStrapServers"] = _kafka.BootstrapServers,
        ["Messaging:GroupID"] = $"franz-test-group-{Guid.NewGuid():N}"
      })
      .Build();
  }

  // ------------------------------------------------------------
  // Hosting registration
  // ------------------------------------------------------------

  [Fact]
  public void AddKafkaHostedListener_registers_listener_and_hosted_service()
  {
    var services = new ServiceCollection();
    var configuration = BuildKafkaConfiguration();

    // 🔑 REQUIRED INFRA (same philosophy as RabbitMQ)
    services.AddLogging();
    services.AddMessagingSerialization();
    services.AddKafkaMessaging(configuration);

    services.AddFranzMediator(new[]
    {
      typeof(KafkaHostingExtensionsTests).Assembly
    });

    // Hosting
    services.AddKafkaHostedListener(_ => { });
    services.AddMongoMessageStore(
          connectionString: _mongo.ConnectionString,
          dbName: _mongo.DatabaseName);
    var provider = services.BuildServiceProvider();

    var listener = provider.GetService<KafkaMessageListener>();
    Assert.NotNull(listener);

    var hostedServices = provider.GetServices<IHostedService>();
    Assert.Contains(hostedServices,
      s => s.GetType() == typeof(KafkaHostedService));
  }

  // ------------------------------------------------------------
  // Options binding
  // ------------------------------------------------------------

  [Fact]
  public void AddKafkaHostedListener_binds_MessagingOptions()
  {
    var services = new ServiceCollection();

    services.AddKafkaHostedListener(opts =>
    {
      opts.BootStrapServers = "kafka-test:9092";
      opts.GroupID = "group-test";
    });

    var provider = services.BuildServiceProvider();

    var options = provider.GetRequiredService<
      Microsoft.Extensions.Options.IOptions<MessagingOptions>>().Value;

    Assert.Equal("kafka-test:9092", options.BootStrapServers);
    Assert.Equal("group-test", options.GroupID);
  }

  // ------------------------------------------------------------
  // Hosted service lifecycle
  // ------------------------------------------------------------

  [Fact]
  public async Task KafkaHostedService_starts_and_stops()
  {
    var configuration = BuildKafkaConfiguration();

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();

        // 🔑 REQUIRED INFRA
        services.AddMessagingSerialization();
        services.AddKafkaMessaging(configuration);
     
        services.AddFranzMediator(new[]
        {
          typeof(KafkaHostingExtensionsTests).Assembly
        });

        // Hosting
        services.AddKafkaHostedListener(_ => { });
      })
      .Build();

    await host.StartAsync();
    await host.StopAsync();
  }

  // ------------------------------------------------------------
  // Outbox hosting (Kafka path)
  // ------------------------------------------------------------

  [Fact]
  public void AddOutboxHostedListener_registers_outbox_listener_and_service()
  {
    var services = new ServiceCollection();
    var configuration = BuildKafkaConfiguration();

    // 🔑 REQUIRED INFRA
    services.AddLogging();
    services.AddMessagingSerialization();
    services.AddKafkaMessaging(configuration);

    services.AddFranzMediator(new[]
    {
      typeof(KafkaHostingExtensionsTests).Assembly
    });
    services.AddMongoMessageStore(
          connectionString: _mongo.ConnectionString,
          dbName: _mongo.DatabaseName);
    services.AddOutboxHostedListener(opts =>
    {
      opts.PollingInterval = TimeSpan.FromMilliseconds(100);
    });

    var provider = services.BuildServiceProvider();

    var listener = provider.GetService<OutboxMessageListener>();
    Assert.NotNull(listener);

    var hostedServices = provider.GetServices<IHostedService>();
    Assert.Contains(hostedServices,
      s => s.GetType() == typeof(OutboxHostedService));
  }

  [Fact]
  public async Task OutboxHostedService_starts_and_stops_with_kafka()
  {
    var configuration = BuildKafkaConfiguration();

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();

        // 🔑 REQUIRED INFRA
        services.AddMessagingSerialization();
        services.AddKafkaMessaging(configuration);
        services.AddMongoMessageStore(
          connectionString: _mongo.ConnectionString,
          dbName: _mongo.DatabaseName);
        services.AddFranzMediator(new[]
        {
          typeof(KafkaHostingExtensionsTests).Assembly
        });

        services.AddOutboxHostedListener(opts =>
        {
          opts.PollingInterval = TimeSpan.FromMilliseconds(100);
        });
      })
      .Build();

    await host.StartAsync();
    await host.StopAsync();
  }
}
