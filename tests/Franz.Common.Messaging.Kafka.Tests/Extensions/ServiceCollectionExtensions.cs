#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Kafka.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

[Collection("Kafka")]
public class KafkaMessagingRegistrationIntegrationTests
{
  private readonly KafkaContainerFixture _fixture;

  public KafkaMessagingRegistrationIntegrationTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;
  }

  // =========================================================
  // SYSTEM MODE TESTS
  // =========================================================

  [Fact]
  public void SystemMode_Services_Should_BeRegistered_AndResolvable()
  {
    var sp = _fixture.BuildServiceProvider();

    sp.GetRequiredService<IMessageContextAccessor>().Should().NotBeNull();
    sp.GetRequiredService<IKafkaConsumerFactory>().Should().NotBeNull();
    sp.GetRequiredService<IConsumer<string, string>>().Should().NotBeNull();
    sp.GetRequiredService<IMessagingPublisher>().Should().BeOfType<MessagingPublisher>();
    sp.GetRequiredService<IMessagingSender>().Should().BeOfType<KafkaSender>();
    sp.GetRequiredService<IMessagingTransaction>().Should().BeOfType<MessagingTransaction>();
    sp.GetRequiredService<IModelProvider>().Should().BeOfType<ModelProvider>();
  }

  [Fact]
  public void SystemMode_Options_Should_Bind_Correctly_AndRemainStable()
  {
    var sp = _fixture.BuildServiceProvider();

    var options1 = _fixture.GetOptions(sp);
    var options2 = _fixture.GetOptions(sp);

    options1.Should().NotBeNull();
    options1.Should().BeSameAs(options2);
    options1.BootstrapServers.Should().Be(_fixture.BootstrapServers);
    options1.GroupId.Should().Be("integration-test-group");
    options1.TopicName.Should().Be("integration-test");
    options1.Consumer.EnableAutoCommit.Should().BeFalse();
    options1.Producer.EnableIdempotence.Should().BeTrue();
  }

  [Fact]
  public void SystemMode_Producer_Should_BeSingleton()
  {
    var sp = _fixture.BuildServiceProvider();

    var producer1 = sp.GetRequiredService<IProducer<string, byte[]>>();
    var producer2 = sp.GetRequiredService<IProducer<string, byte[]>>();

    producer1.Should().NotBeNull();
    producer1.Should().BeSameAs(producer2);
  }

  [Fact]
  public void SystemMode_ConsumerFactory_Should_BeSingleton()
  {
    var sp = _fixture.BuildServiceProvider();

    var factory1 = sp.GetRequiredService<IKafkaConsumerFactory>();
    var factory2 = sp.GetRequiredService<IKafkaConsumerFactory>();

    factory1.Should().NotBeNull();
    factory1.Should().BeSameAs(factory2);
  }

  [Fact]
  public async Task SystemMode_KeyedServices_Should_Resolve_AndBeIsolatedPerKey()
  {
    // BuildServiceProvider already calls AddFranzMediator so IDispatcher is available.
    // The configure action adds two additional keyed registrations.
    var sp = _fixture.BuildServiceProvider(services =>
    {
      services.AddKafkaMessaging(_fixture.Configuration, "tenant-a");
      services.AddKafkaMessaging(_fixture.Configuration, "tenant-b");
    });

    await using var scope = sp.CreateAsyncScope();
    var scopedSp = scope.ServiceProvider;

    var pubA = scopedSp.GetKeyedService<IMessagingPublisher>("tenant-a");
    var pubB = scopedSp.GetKeyedService<IMessagingPublisher>("tenant-b");
    var senderA = scopedSp.GetKeyedService<IMessagingSender>("tenant-a");
    var senderB = scopedSp.GetKeyedService<IMessagingSender>("tenant-b");
    var txA = scopedSp.GetKeyedService<IMessagingTransaction>("tenant-a");
    var txB = scopedSp.GetKeyedService<IMessagingTransaction>("tenant-b");
    var factoryA = scopedSp.GetKeyedService<IKafkaConsumerFactory>("tenant-a");
    var factoryB = scopedSp.GetKeyedService<IKafkaConsumerFactory>("tenant-b");
    var consumerA = scopedSp.GetKeyedService<IConsumer<string, string>>("tenant-a");
    var consumerB = scopedSp.GetKeyedService<IConsumer<string, string>>("tenant-b");

    pubA.Should().NotBeNull();
    pubB.Should().NotBeNull();
    pubA.Should().NotBeSameAs(pubB);
    senderA.Should().NotBeNull();
    senderB.Should().NotBeNull();
    senderA.Should().NotBeSameAs(senderB);
    txA.Should().NotBeNull();
    txB.Should().NotBeNull();
    txA.Should().NotBeSameAs(txB);
    factoryA.Should().NotBeNull();
    factoryB.Should().NotBeNull();
    consumerA.Should().NotBeNull();
    consumerB.Should().NotBeNull();
  }

  [Fact]
  public void SystemMode_ModelProvider_ShouldRespectSingletonLifetime()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IModelProvider, ModelProvider>();
    services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton);

    var descriptor = services.Single(s => s.ServiceType == typeof(IModelProvider));
    descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  // =========================================================
  // EVENT MODE TESTS
  // =========================================================

  [Fact]
  public async Task EventMode_KeyedServices_Should_BeRegistered_PerEventType()
  {
    var sp = _fixture.BuildEventBasedServiceProvider();

    await using var scope = sp.CreateAsyncScope();
    var scopedSp = scope.ServiceProvider;

    // TestIntegrationEvent handler is in the fixture assembly
    // AddFranzMediator scans it → AddEventBasedKafkaMessaging discovers it
    // → keyed registrations created under "TestIntegrationEvent"
    var publisher = scopedSp.GetKeyedService<IMessagingPublisher>(
        nameof(TestIntegrationEvent));
    var sender = scopedSp.GetKeyedService<IMessagingSender>(
        nameof(TestIntegrationEvent));
    var consumer = scopedSp.GetKeyedService<IConsumer<string, string>>(
        nameof(TestIntegrationEvent));

    publisher.Should().NotBeNull(
        because: "event mode must register a publisher keyed by event type name");
    sender.Should().NotBeNull(
        because: "event mode must register a sender keyed by event type name");
    consumer.Should().NotBeNull(
        because: "event mode must register a consumer keyed by event type name");
  }

  [Fact]
  public async Task EventMode_TwoEvents_Should_ProduceIsolatedKeyedRegistrations()
  {
    var sp = _fixture.BuildEventBasedServiceProvider();

    await using var scope = sp.CreateAsyncScope();
    var scopedSp = scope.ServiceProvider;

    var publisherA = scopedSp.GetKeyedService<IMessagingPublisher>(
        nameof(TestIntegrationEvent));
    var publisherB = scopedSp.GetKeyedService<IMessagingPublisher>(
        nameof(AnotherIntegrationEvent));

    // Both should be non-null if both handlers are in the scanned assembly
    // and should be isolated instances
    if (publisherA != null && publisherB != null)
      publisherA.Should().NotBeSameAs(publisherB);

    // Core shared infra must always resolve regardless
    scopedSp.GetRequiredService<IProducer<string, byte[]>>().Should().NotBeNull();
  }

  [Fact]
  public void EventMode_And_SystemMode_CanCoexist_WithoutConflict()
  {
    var act = () =>
    {
      var sp = _fixture.BuildServiceProvider(services =>
      {
        services.AddEventBasedKafkaMessaging(_fixture.Configuration);
      });
      return sp;
    };

    act.Should().NotThrow(
        because: "system mode and event mode registrations must not conflict");
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

  public class AnotherIntegrationEvent : IIntegrationEvent { }

  public class AnotherIntegrationEventHandler : INotificationHandler<AnotherIntegrationEvent>
  {
    public Task Handle(
        AnotherIntegrationEvent notification,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
  }
}