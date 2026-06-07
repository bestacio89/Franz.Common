#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Fixtures;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  public IConfiguration Configuration { get; private set; } = default!;

  private readonly KafkaContainer _container =
      new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
          .WithCleanUp(true)
          .Build();

  public string BootstrapServers => _container.GetBootstrapAddress();

  public async Task InitializeAsync() => await _container.StartAsync();

  public async Task DisposeAsync()
  {
    await _container.StopAsync();
    await _container.DisposeAsync();
  }

  // =========================================================
  // SYSTEM MODE provider
  // =========================================================

  public IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure = null)
  {
    var services = new ServiceCollection();
    var topicName = "integration-test";
    var configuration = BuildConfiguration(topicName);

    CreateTopicAsync(topicName).GetAwaiter().GetResult();

    RegisterCoreServices(services, configuration);

    // AddFranzMediator before AddKafkaMessaging so IDispatcher is available
    // when keyed MessagingPublisher factories resolve it
    services.AddFranzMediator(new[] { typeof(KafkaContainerFixture).Assembly });
    services.AddKafkaMessaging(configuration);

    configure?.Invoke(services);

    return services.BuildServiceProvider();
  }

  // =========================================================
  // EVENT MODE provider
  // AddFranzMediator MUST come before AddEventBasedKafkaMessaging
  // so DiscoverHandledEventTypes() finds registered handlers
  // =========================================================

  public IServiceProvider BuildEventBasedServiceProvider(
      Action<IServiceCollection>? configure = null)
  {
    var services = new ServiceCollection();
    var configuration = BuildConfiguration("integration-test-event-mode");

    RegisterCoreServices(services, configuration);

    // Order matters — mediator first, event messaging second
    services.AddFranzMediator(new[] { typeof(KafkaContainerFixture).Assembly });
    services.AddEventBasedKafkaMessaging(configuration);

    configure?.Invoke(services);

    return services.BuildServiceProvider();
  }

  // =========================================================
  // HELPERS
  // =========================================================

  public KafkaMessagingOptions GetOptions(IServiceProvider sp)
      => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaMessagingOptions>>()
           .Value;

  private IConfiguration BuildConfiguration(string topicName)
  {
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          [$"{KafkaMessagingOptions.SectionName}:BootstrapServers"] = BootstrapServers,
          [$"{KafkaMessagingOptions.SectionName}:TopicName"] = topicName,
          [$"{KafkaMessagingOptions.SectionName}:GroupId"] = "integration-test-group",
          [$"{KafkaMessagingOptions.SectionName}:Consumer:AutoOffsetReset"] = "Earliest",
          [$"{KafkaMessagingOptions.SectionName}:Consumer:EnableAutoCommit"] = "false",
          [$"{KafkaMessagingOptions.SectionName}:Producer:Acks"] = "All",
          [$"{KafkaMessagingOptions.SectionName}:Producer:EnableIdempotence"] = "true",
        })
        .Build();

    Configuration = config;
    return config;
  }

  private static void RegisterCoreServices(
      IServiceCollection services,
      IConfiguration configuration)
  {
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
    services.AddNoDuplicateScoped<IAssemblyAccessor, AssemblyAccessorWrapper>();
    services.AddDefaultMessageSerializer();
  }

  private async Task CreateTopicAsync(string topicName)
  {
    using var adminClient = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = BootstrapServers
    }).Build();

    try
    {
      await adminClient.CreateTopicsAsync(new TopicSpecification[]
      {
                new() { Name = topicName, ReplicationFactor = 1, NumPartitions = 1 }
      });
    }
    catch (CreateTopicsException e)
        when (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
    {
      // Idempotent — topic already exists, nothing to do
    }
  }
}