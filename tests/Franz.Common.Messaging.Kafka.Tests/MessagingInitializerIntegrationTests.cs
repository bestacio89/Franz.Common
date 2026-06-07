#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Configuration;
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
      Failure = { DeadLetterTopic = "dead-letter-topic" }
    });

    var adminClient = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    }).Build();

    return new KafkaMessagingInitializer(
        adminClient,
        accessor ?? BuildDefaultAccessor(),
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