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
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  [Collection("Kafka")]
  public sealed class KafkaMessagingInitializerIntegrationTests : IAsyncLifetime
  {
    private readonly KafkaContainerFixture _fixture;

    public KafkaMessagingInitializerIntegrationTests(KafkaContainerFixture fixture)
    {
      _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    private KafkaMessagingInitializer CreateSut(KafkaMessagingOptions? optionsOverride = null)
    {
      var options = Options.Create(optionsOverride ?? new KafkaMessagingOptions
      {
        GroupId = "test-group",
        BootstrapServers = _fixture.BootstrapServers,
        TopicName = "main-topic",
        Failure = { DeadLetterTopic = "dead-letter-topic" }
      });

      var accessorMock = new Mock<IAssemblyAccessor>();
      var mockAssembly = new Mock<IAssembly>();
      mockAssembly.Setup(a => a.Name).Returns("Franz.TestProject.Api");
      mockAssembly.Setup(a => a.Assembly).Returns(typeof(KafkaMessagingInitializer).Assembly);
      accessorMock.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

      var adminClient = new AdminClientBuilder(new AdminClientConfig
      {
        BootstrapServers = _fixture.BootstrapServers
      }).Build();

      return new KafkaMessagingInitializer(adminClient, accessorMock.Object, options);
    }

    private static void ResetInitializerState()
    {
      var field = typeof(KafkaMessagingInitializer)
          .GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
      field?.SetValue(null, 0);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateConfiguredAndDiscoveredTopics()
    {
      ResetInitializerState();

      var sut = CreateSut();

      Func<Task> act = async () => await sut.InitializeAsync();

      await act.Should().NotThrowAsync();

      // Verify topics exist
      using var admin = new AdminClientBuilder(
          new AdminClientConfig { BootstrapServers = _fixture.BootstrapServers }).Build();

      var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
      metadata.Topics.Should().Contain(t => t.Topic == "main-topic");
      metadata.Topics.Should().Contain(t => t.Topic == "dead-letter-topic");
    }

    [Fact]
    public async Task InitializeAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
      ResetInitializerState();

      var sut = CreateSut();

      await sut.InitializeAsync();

      Func<Task> act = async () => await sut.InitializeAsync();

      await act.Should().NotThrowAsync("Second call should be ignored by Interlocked guard.");
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateIntegrationEventTopic()
    {
      ResetInitializerState();

      // Dummy integration event for test
      var eventTopicName = "test-integration-event-topic";

      var accessorMock = new Mock<IAssemblyAccessor>();
      var mockAssembly = new Mock<IAssembly>();
      mockAssembly.Setup(a => a.Name).Returns("Franz.TestProject.Api");
      mockAssembly.Setup(a => a.Assembly).Returns(typeof(TestIntegrationEventHandler).Assembly);
      accessorMock.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

      var options = new KafkaMessagingOptions
      {
        GroupId = "test-group",
        BootstrapServers = _fixture.BootstrapServers,
        TopicName = "main-topic",
        Failure = { DeadLetterTopic = "dead-letter-topic" }
      };

      var sut = new KafkaMessagingInitializer(
          new AdminClientBuilder(new AdminClientConfig
          {
            BootstrapServers = _fixture.BootstrapServers
          }).Build(),
          accessorMock.Object,
          Options.Create(options));

      await sut.InitializeAsync();

      // Verify discovered integration event topic was created
      using var admin = new AdminClientBuilder(
          new AdminClientConfig { BootstrapServers = _fixture.BootstrapServers }).Build();

      var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
      var discoveredTopic = TopicNamer.GetTopicName(new AssemblyWrapper(typeof(TestIntegrationEventHandler).Assembly));
      metadata.Topics.Should().Contain(t => t.Topic == discoveredTopic);
    }

    // Dummy integration event and handler for discovery
    public class TestIntegrationEvent : IIntegrationEvent { }

    public class TestIntegrationEventHandler : INotificationHandler<TestIntegrationEvent>
    {
      public Task Handle(TestIntegrationEvent notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
  }
}