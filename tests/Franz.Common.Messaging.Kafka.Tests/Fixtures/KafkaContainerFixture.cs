#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNet.Testcontainers.Builders;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Testcontainers.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Fixtures;

public sealed class KafkaContainerFixture : IAsyncLifetime
{
  public IConfiguration Configuration { get; private set; } = default!;
  private readonly KafkaContainer _container = new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
      .WithCleanUp(true)
      .Build();

  public string BootstrapServers => _container.GetBootstrapAddress();

  public async Task InitializeAsync() => await _container.StartAsync();

  public async Task DisposeAsync()
  {
    await _container.StopAsync();
    await _container.DisposeAsync();
  }

  public IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure = null)
  {
    
    var services = new ServiceCollection();
    var topicName = "integration-test";

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:BootstrapServers"] = BootstrapServers,
          ["Messaging:Kafka:TopicName"] = topicName,
          ["Messaging:Kafka:GroupId"] = "integration-test-group",
          ["Messaging:Kafka:Consumer:AutoOffsetReset"] = "Earliest",
          ["Messaging:Kafka:Consumer:EnableAutoCommit"] = "false",
          ["Messaging:Kafka:Producer:Acks"] = "All",
        })
        .Build();

    // Ensure topic exists before returning the provider
    CreateTopicAsync(topicName).GetAwaiter().GetResult();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    services.AddFranzMediator(new[] { typeof(KafkaContainerFixture).Assembly });
    services.AddNoDuplicateScoped<IAssemblyAccessor, AssemblyAccessorWrapper>();
    services.AddDefaultMessageSerializer();
    services.AddKafkaMessaging(configuration);

    configure?.Invoke(services);

    return services.BuildServiceProvider();
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
    catch (CreateTopicsException e) when (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
    {
      // Ignore if topic exists
    }
  }

  public KafkaMessagingOptions GetOptions(IServiceProvider sp)
      => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaMessagingOptions>>().Value;
}