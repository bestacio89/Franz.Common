#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

[Collection("Kafka")]
public sealed class KafkaMessagingInitializerTests
{
  private readonly KafkaContainerFixture _fixture;
  private readonly string _bootstrapServers;

  public KafkaMessagingInitializerTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    _bootstrapServers = _fixture.BootstrapServers;
  }

  private KafkaMessagingInitializer CreateSut(
      Mock<IAdminClient>? adminClientMock = null,
      Mock<IAssemblyAccessor>? assemblyAccessorMock = null)
  {
    var options = Options.Create(new KafkaMessagingOptions { BootStrapServers = _bootstrapServers });

    var accessor = assemblyAccessorMock ?? new Mock<IAssemblyAccessor>();
    var mockAssembly = new Mock<IAssembly>();

    mockAssembly.Setup(a => a.Name).Returns("Franz.TestProject.Api");
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(KafkaMessagingInitializer).Assembly);

    accessor.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

    var admin = adminClientMock?.Object ??
                new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();

    return new KafkaMessagingInitializer(admin, accessor.Object, options);
  }

  [Fact]
  public async Task InitializeAsync_ShouldCreateTopicsSuccessfully_OnFirstRun()
  {
    ResetInitializerState();
    var sut = CreateSut();

    Func<Task> act = async () => await sut.InitializeAsync();

    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task InitializeAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
  {
    ResetInitializerState();
    var sut = CreateSut();

    await sut.InitializeAsync();

    Func<Task> act = async() => await sut.InitializeAsync();

    await act.Should().NotThrowAsync("Second call should hit the Interlocked guard and return immediately.");
  }

  [Fact]
  public async Task InitializeAsync_ShouldHandleTopicAlreadyExists_WithoutThrowing()
  {
    ResetInitializerState();

    var topicName = "common-in";

    using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
    try
    {
      await admin.CreateTopicsAsync(new[]
      {
                new TopicSpecification { Name = topicName, NumPartitions = 1, ReplicationFactor = 1 }
            });
    }
    catch { /* ignore if already exists */ }

    var sut = CreateSut();

    Func<Task> act = async () => await sut.InitializeAsync();

    await act.Should().NotThrowAsync("EnsureTopicAsync must swallow TopicAlreadyExists errors.");
  }

  [Fact]
  public async Task InitializeAsync_WhenAdminClientThrowsUnknownError_ShouldPropagateException()
  {
    ResetInitializerState();

    var adminMock = new Mock<IAdminClient>();
    var reports = new List<CreateTopicReport>
        {
            new()
            {
                Topic = "bad-topic",
                Error = new Error(ErrorCode.TopicException, "Simulated Broker Failure")
            }
        };
    var kafkaException = new CreateTopicsException(reports);

    adminMock
        .Setup(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()))
        .ThrowsAsync(kafkaException);

    var sut = CreateSut(adminClientMock: adminMock);

    Func<Task> act = async () => await sut.InitializeAsync();

    await act.Should().ThrowAsync<CreateTopicsException>();
    adminMock.Verify(x => x.CreateTopicsAsync(It.IsAny<IEnumerable<TopicSpecification>>(), It.IsAny<CreateTopicsOptions>()), Times.AtLeastOnce);
  }

  [Fact]
  public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException()
  {
    var admin = new Mock<IAdminClient>().Object;
    var accessor = new Mock<IAssemblyAccessor>().Object;
    var options = Options.Create(new KafkaMessagingOptions());

    Assert.Throws<ArgumentNullException>(() => new KafkaMessagingInitializer(null!, accessor, options));
    Assert.Throws<ArgumentNullException>(() => new KafkaMessagingInitializer(admin, null!, options));
    Assert.Throws<ArgumentNullException>(() => new KafkaMessagingInitializer(admin, accessor, null!));
  }

  private static void ResetInitializerState()
  {
    // Reset the static "_initialized" field for isolation
    var field = typeof(KafkaMessagingInitializer).GetField("_initialized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    field?.SetValue(null, 0);
  }
}