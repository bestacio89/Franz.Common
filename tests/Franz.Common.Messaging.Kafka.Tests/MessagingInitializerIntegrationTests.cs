#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Initializer
{
  [Collection("Kafka")] // ensures fixture is shared
  public class MessagingInitializerIntegrationTests
  {
    private readonly KafkaContainerFixture _fixture;

    public MessagingInitializerIntegrationTests(KafkaContainerFixture fixture)
    {
      _fixture = fixture;
    }

    [Fact]
    public void Initialize_CreatesTopicsOnKafka_UsingFixture()
    {
      // Arrange
      var adminConfig = new AdminClientConfig { BootstrapServers = _fixture.BootstrapServers };
      using var adminClient = new AdminClientBuilder(adminConfig).Build();

      var assemblyAccessor = new AssemblyAccessorWrapper();
      var options = Options.Create(new MessagingOptions { BootStrapServers = _fixture.BootstrapServers });

      var initializer = new MessagingInitializer(adminClient, assemblyAccessor, options);

      // Act
      initializer.Initialize();

      // Assert: Topics exist
      var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
      var topicName = TopicNamer.GetTopicName(assemblyAccessor.GetEntryAssembly());
      var dlqName = TopicNamer.GetDeadLetterTopicName(assemblyAccessor.GetEntryAssembly());

      Assert.Contains(metadata.Topics, t => t.Topic == topicName);
      Assert.Contains(metadata.Topics, t => t.Topic == dlqName);
    }
  }
}