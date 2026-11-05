#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

[Collection("Kafka")]
public sealed class KafkaMessagingInitializerIntegrationTests
{
  private readonly KafkaContainerFixture _fixture;

  public KafkaMessagingInitializerIntegrationTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
  }

  // =========================================================
  // FACTORY HELPERS
  // =========================================================

  private KafkaMessagingInitializer CreateSut(
      IAssemblyAccessor? accessor = null,
      KafkaMessagingOptions? options = null,
      bool perEvent = false)
  {
    var opts = Options.Create(options ?? new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = "main-topic",
      DeadLetterTopic = "dead-letter-topic"
    });

    var adminClient = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    }).Build();

    return new KafkaMessagingInitializer(
        adminClient,
        new KafkaTopicNames(accessor ?? BuildDefaultAccessor(), opts),
        opts,
        perEvent);
  }

  private static IAssemblyAccessor BuildDefaultAccessor()
  {
    var accessorMock = new Mock<IAssemblyAccessor>();
    var mockAssembly = new Mock<IAssembly>();

    mockAssembly.Setup(a => a.Name).Returns("Franz.TestProject.Api");
    mockAssembly.Setup(a => a.Assembly)
        .Returns(typeof(KafkaMessagingInitializer).Assembly);

    accessorMock.Setup(a => a.GetEntryAssembly())
        .Returns(mockAssembly.Object);

    return accessorMock.Object;
  }

  // Accessor that returns the TEST assembly as entry assembly.
  // Company prefix = "Franz" which matches Franz.Common.Messaging.Kafka.Tests
  // so DiscoverIntegrationEventTopics() finds TestIntegrationEventHandler.
  private static IAssemblyAccessor BuildIntegrationAccessor()
  {
    var accessorMock = new Mock<IAssemblyAccessor>();
    var mockAssembly = new Mock<IAssembly>();

    // Use "Franz.TestProject.Api" — prefix "Franz" matches the test assembly
    mockAssembly.Setup(a => a.Name).Returns("Franz.TestProject.Api");
    mockAssembly.Setup(a => a.Assembly)
        .Returns(typeof(TestIntegrationEventHandler).Assembly);

    accessorMock.Setup(a => a.GetEntryAssembly())
        .Returns(mockAssembly.Object);

    return accessorMock.Object;
  }

  private IAdminClient BuildAdminClient()
      => new AdminClientBuilder(new AdminClientConfig
      {
        BootstrapServers = _fixture.BootstrapServers
      }).Build();


  private async Task DeleteTopicIfExistsAsync(string topicName)
  {
    using var admin = BuildAdminClient();
    var metadata = admin.GetMetadata(topicName, TimeSpan.FromSeconds(10));
    var existingTopic = metadata.Topics.SingleOrDefault(topic => topic.Topic == topicName);

    if (existingTopic is null || existingTopic.Error.Code == ErrorCode.UnknownTopicOrPart)
      return;

    existingTopic.Error.Code.Should().Be(ErrorCode.NoError);

    await admin.DeleteTopicsAsync(
        new[] { topicName },
        new DeleteTopicsOptions
        {
          OperationTimeout = TimeSpan.FromSeconds(10),
          RequestTimeout = TimeSpan.FromSeconds(10)
        });

    var deadline = DateTime.UtcNow.AddSeconds(10);
    while (DateTime.UtcNow < deadline)
    {
      metadata = admin.GetMetadata(topicName, TimeSpan.FromSeconds(10));
      var remainingTopic = metadata.Topics.SingleOrDefault(topic => topic.Topic == topicName);

      if (remainingTopic is null || remainingTopic.Error.Code == ErrorCode.UnknownTopicOrPart)
        return;

      await Task.Delay(TimeSpan.FromMilliseconds(100));
    }

    throw new TimeoutException($"Kafka topic '{topicName}' was not deleted within the expected timeout.");
  }


  [Fact]
  public async Task InitializeAsync_ShouldApplyConfiguredPartitionCountAndReplicationFactor()
  {
    var options = new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = "configured-topic",
      DeadLetterTopic = "configured-topic-dlt",
      Topic = new KafkaTopicOptions
      {
        NumPartitions = 3,
        ReplicationFactor = 1,
        Configurations = new Dictionary<string, string> { ["cleanup.policy"] = "compact" }
      }
    };

    var sut = CreateSut(options: options);
    await sut.InitializeAsync();

    using var admin = BuildAdminClient();
    var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));
    var topic = metadata.Topics.Should().ContainSingle(item => item.Topic == "configured-topic").Which;

    topic.Partitions.Should().HaveCount(3);
    topic.Partitions.Should().OnlyContain(partition => partition.Replicas.Count() == 1);

    var config = await admin.DescribeConfigsAsync(new[]
    {
      new ConfigResource { Name = "configured-topic", Type = ResourceType.Topic }
    });
    config.Should().ContainSingle();
    config[0].Entries["cleanup.policy"].Value.Should().Be("compact");
  }

  [Fact]
  public async Task InitializeAsync_ShouldRespectCancellationBeforeProvisioning()
  {
    var topicName = $"cancellation-{Guid.NewGuid():N}";
    var deadLetterTopicName = $"{topicName}-dlt";
    var sut = CreateSut(options: new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = topicName,
      DeadLetterTopic = deadLetterTopicName
    });
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    var action = () => sut.InitializeAsync(cancellation.Token).AsTask();

    await action.Should().ThrowAsync<OperationCanceledException>();
    using var admin = BuildAdminClient();
    admin.GetMetadata(TimeSpan.FromSeconds(10)).Topics
        .Should().NotContain(topic => topic.Topic == topicName);
  }


  [Fact]
  public async Task InitializeAsync_VerifyMode_ShouldAcceptMorePartitionsThanConfiguredMinimum()
  {
    var topicName = $"verify-partitions-{Guid.NewGuid():N}";
    var deadLetterTopicName = $"{topicName}-dlt";
    var create = CreateSut(options: new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = topicName,
      DeadLetterTopic = deadLetterTopicName,
      Topic = new KafkaTopicOptions { NumPartitions = 3, ReplicationFactor = 1 }
    });
    await create.InitializeAsync();

    var verify = CreateSut(options: new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = topicName,
      DeadLetterTopic = deadLetterTopicName,
      ProvisioningMode = ProvisioningMode.Verify,
      Topic = new KafkaTopicOptions { NumPartitions = 2, ReplicationFactor = 1 },
      EventTopicOptions = new Dictionary<string, KafkaTopicOptions>(StringComparer.Ordinal)
      {
        ["common-in"] = new KafkaTopicOptions { NumPartitions = 1, ReplicationFactor = 1 }
      }
    });

    await verify.InitializeAsync();
  }

  [Fact]
  public async Task InitializeAsync_ShouldRejectReplicationFactorAboveBrokerCount()
  {
    var sut = CreateSut(options: new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = $"replication-factor-{Guid.NewGuid():N}",
      DeadLetterTopic = $"replication-factor-{Guid.NewGuid():N}-dlt",
      Topic = new KafkaTopicOptions { ReplicationFactor = 2 }
    });

    var act = () => sut.InitializeAsync().AsTask();

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*exceeds the available broker count*");
  }

  // =========================================================
  // SYSTEM MODE TESTS
  // =========================================================

  [Fact]
  public async Task InitializeAsync_ShouldCreateConfiguredTopics()
  {
    var sut = CreateSut(perEvent: false);
    await sut.InitializeAsync();

    using var admin = BuildAdminClient();
    var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));

    metadata.Topics.Should().Contain(t => t.Topic == "main-topic");
    metadata.Topics.Should().Contain(t => t.Topic == "dead-letter-topic");
  }

  [Fact]
  public async Task InitializeAsync_ShouldBeIdempotent()
  {
    var sut = CreateSut(perEvent: false);

    await sut.InitializeAsync();
    await sut.InitializeAsync();

    true.Should().BeTrue();
  }

  [Fact]
  public void GeneratedManifest_ShouldIncludeIntegrationEventDestinationsAndSubscriptions()
  {
    var manifest = Franz.Common.Messaging.Manifest.MessagingManifestRegistry.Get(
        typeof(TestIntegrationEventHandler).Assembly);

    manifest.Destinations.Should().Contain(typeof(TestIntegrationEvent));
    manifest.Subscriptions.Should().Contain(typeof(TestIntegrationEvent));
  }

  [Fact]
  public async Task InitializeAsync_SystemMode_ShouldCreateAssemblyBasedIntegrationEventTopic()
  {
    var sut = CreateSut(BuildIntegrationAccessor(), perEvent: false);
    await sut.InitializeAsync();

    var expectedTopic = TopicNamer.GetTopicName(
        new AssemblyWrapper(typeof(TestIntegrationEventHandler).Assembly));

    using var admin = BuildAdminClient();
    var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));

    metadata.Topics.Should().Contain(t => t.Topic == expectedTopic);
  }


  [Fact]
  public async Task InitializeAsync_ShouldApplyDeadLetterSpecificTopicOptions()
  {
    var topicName = $"topic-options-{Guid.CreateVersion7():N}";
    var deadLetterTopicName = $"{topicName}-dlt";
    var sut = CreateSut(options: new KafkaMessagingOptions
    {
      GroupId = "test-group",
      BootstrapServers = _fixture.BootstrapServers,
      TopicName = topicName,
      DeadLetterTopic = deadLetterTopicName,
      Topic = new KafkaTopicOptions { NumPartitions = 1, ReplicationFactor = 1 },
      DeadLetterTopicOptions = new KafkaTopicOptions { NumPartitions = 2, ReplicationFactor = 1 }
    });

    await sut.InitializeAsync();

    using var admin = BuildAdminClient();
    var metadata = admin.GetMetadata(deadLetterTopicName, TimeSpan.FromSeconds(10));
    metadata.Topics.Single(topic => topic.Topic == deadLetterTopicName).Partitions.Should().HaveCount(2);
  }

  [Fact]
  public async Task InitializeAsync_EventMode_ShouldApplyPerEventTopicOptions()
  {
    // This assertion requires the topic to be absent before provisioning. Use a
    // dedicated broker so other initializer tests cannot pre-create manifest topics.
    var isolatedFixture = new KafkaContainerFixture();
    await isolatedFixture.InitializeAsync();

    try
    {
      var eventType = typeof(KafkaMessagingRegistrationIntegrationTests.AnotherIntegrationEvent);
      var eventTopic = TopicNamer.GetTopicName(eventType);
      var options = new KafkaMessagingOptions
      {
        GroupId = "test-group",
        BootstrapServers = isolatedFixture.BootstrapServers,
        TopicName = $"event-options-{Guid.CreateVersion7():N}",
        DeadLetterTopic = $"event-options-{Guid.CreateVersion7():N}-dlt",
        Topic = new KafkaTopicOptions { NumPartitions = 1, ReplicationFactor = 1 },
        EventTopicOptions = new Dictionary<string, KafkaTopicOptions>(StringComparer.Ordinal)
        {
          [eventTopic] = new KafkaTopicOptions { NumPartitions = 3, ReplicationFactor = 1 }
        }
      };
      var configuredOptions = Options.Create(options);
      using var admin = new AdminClientBuilder(new AdminClientConfig
      {
        BootstrapServers = isolatedFixture.BootstrapServers
      }).Build();
      var sut = new KafkaMessagingInitializer(
          admin,
          new KafkaTopicNames(BuildIntegrationAccessor(), configuredOptions),
          configuredOptions,
          perEvent: true);

      await sut.InitializeAsync();

      var metadata = admin.GetMetadata(eventTopic, TimeSpan.FromSeconds(10));
      metadata.Topics.Single(topic => topic.Topic == eventTopic).Partitions.Should().HaveCount(3);
    }
    finally
    {
      await isolatedFixture.DisposeAsync();
    }
  }

  // =========================================================
  // EVENT MODE TESTS
  // =========================================================

  [Fact]
  public async Task InitializeAsync_EventMode_ShouldCreateEventTypeBasedTopic()
  {
    var sut = CreateSut(BuildIntegrationAccessor(), perEvent: true);
    await sut.InitializeAsync();

    // TestIntegrationEvent → "test-integration-in"
    var expectedTopic = TopicNamer.GetTopicName(typeof(TestIntegrationEvent));
    var expectedDlt = TopicNamer.GetDeadLetterTopicName(typeof(TestIntegrationEvent));

    using var admin = BuildAdminClient();
    var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));

    metadata.Topics.Should().Contain(t => t.Topic == expectedTopic,
        because: $"event mode must create topic '{expectedTopic}' for TestIntegrationEvent");

    metadata.Topics.Should().Contain(t => t.Topic == expectedDlt,
        because: $"event mode must create DLT '{expectedDlt}' for TestIntegrationEvent");
  }

  [Fact]
  public async Task InitializeAsync_EventMode_TopicNameDiffersFromSystemMode()
  {
    var systemTopic = TopicNamer.GetTopicName(
        new AssemblyWrapper(typeof(TestIntegrationEventHandler).Assembly));

    var eventTopic = TopicNamer.GetTopicName(typeof(TestIntegrationEvent));

    systemTopic.Should().NotBe(eventTopic,
        because: "system and event modes must produce distinct topic names");
  }

  [Fact]
  public async Task InitializeAsync_EventMode_ShouldBeIdempotent()
  {
    var sut = CreateSut(BuildIntegrationAccessor(), perEvent: true);

    await sut.InitializeAsync();
    await sut.InitializeAsync();

    true.Should().BeTrue();
  }

  // =========================================================
  // TEST DOMAIN TYPES
  // =========================================================

  public class TestIntegrationEvent : IIntegrationEvent { }

  public class TestIntegrationEventHandler : INotificationHandler<TestIntegrationEvent>
  {
    public Task Handle(
        TestIntegrationEvent notification,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
  }
}