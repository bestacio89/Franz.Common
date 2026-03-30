#nullable enable
using Confluent.Kafka;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Fixtures;

public sealed class KafkaTestSetup
{
  public IServiceCollection Services { get; }
  public IServiceProvider ServiceProvider { get; }

  public KafkaTestSetup(string bootstrapServers)
  {
    // 1. Build in-memory configuration for KafkaMessagingOptions
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:BootStrapServers"] = bootstrapServers,
          ["Messaging:Kafka:GroupID"] = "test-group",
          ["Messaging:Kafka:TopicName"] = "test-topic",
          ["Messaging:Kafka:DeadLetterTopicName"] = "test-dead-letter",
          ["Messaging:Kafka:Partitions"] = "1",
          ["Messaging:Kafka:ReplicationFactor"] = "1"
        })
        .Build();

    // 2. Setup DI
    Services = new ServiceCollection();
    Services.AddFranzMediator(new[] { typeof(KafkaContainerFixture).Assembly });
    Services.AddLogging();
    Services.AddOptions();
    Services.AddKafkaMessagingOptions(configuration);

    // 3. Add Kafka messaging services for DI testing
    Services.AddKafkaMessaging(configuration);

    ServiceProvider = Services.BuildServiceProvider();
  }

  public KafkaMessagingOptions GetKafkaOptions()
      => ServiceProvider.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
}

// 1. Core/Shared Logic Collection
[CollectionDefinition("Kafka")] public class KafkaCollection : ICollectionFixture<KafkaContainerFixture> 
{ } 
// 2. Dedicated Sender/Producer Collection
[CollectionDefinition("KafkaSender")] public class KafkaSenderCollection : ICollectionFixture<KafkaContainerFixture> { } 

// 3. Dedicated Low-Level Consumer Collection

[CollectionDefinition("KafkaConsumer")] public class KafkaConsumerCollection : ICollectionFixture<KafkaContainerFixture> { }

[CollectionDefinition("KafkaConnections")]
public class KafkaConnectionsCollection : ICollectionFixture<KafkaContainerFixture>
{ }

[CollectionDefinition("KafkaIntegration")]
public class KafkaIntegrationsCollection : ICollectionFixture<KafkaContainerFixture>
{ }