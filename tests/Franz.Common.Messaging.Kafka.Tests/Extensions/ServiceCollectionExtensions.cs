#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Kafka.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  [Collection("Kafka")]
  public class KafkaMessagingRegistrationIntegrationTests
  {
    private readonly KafkaContainerFixture _fixture;

    public KafkaMessagingRegistrationIntegrationTests(KafkaContainerFixture fixture)
    {
      _fixture = fixture;
    }

    [Fact]
    public void Services_Should_BeRegistered_AndResolvable()
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
    public void Options_Should_Bind_Correctly_FromFixture_AndRemainStable()
    {
      var sp = _fixture.BuildServiceProvider();

      var options1 = _fixture.GetOptions(sp);
      var options2 = _fixture.GetOptions(sp);

      options1.Should().NotBeNull();
      options2.Should().NotBeNull();

      options1.Should().BeSameAs(options2);

      options1.BootstrapServers.Should().Be(_fixture.BootstrapServers);
      options1.GroupId.Should().Be("integration-test-group");
      options1.TopicName.Should().Be("integration-test");

      options1.Consumer.EnableAutoCommit.Should().BeFalse();
      options1.Producer.EnableIdempotence.Should().BeTrue();
    }

    [Fact]
    public void Producer_Should_BeSingleton_AndResolvable()
    {
      var sp = _fixture.BuildServiceProvider();

      var producer1 = sp.GetRequiredService<IProducer<string, byte[]>>();
      var producer2 = sp.GetRequiredService<IProducer<string, byte[]>>();

      producer1.Should().NotBeNull();
      producer2.Should().NotBeNull();
      producer1.Should().BeSameAs(producer2);
    }

    [Fact]
    public void ConsumerFactory_Should_BeSingleton_AndStateless()
    {
      var sp = _fixture.BuildServiceProvider();

      var factory1 = sp.GetRequiredService<IKafkaConsumerFactory>();
      var factory2 = sp.GetRequiredService<IKafkaConsumerFactory>();

      factory1.Should().NotBeNull();
      factory2.Should().NotBeNull();
      factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void KeyedServices_Should_Resolve_And_Be_Isolated_PerKey()
    {
      var sp = _fixture.BuildServiceProvider(services =>
      {
        services.AddKafkaMessaging(_fixture.Configuration, "tenant-a");
        services.AddKafkaMessaging(_fixture.Configuration, "tenant-b");
      });

      var pubA = sp.GetKeyedService<IMessagingPublisher>("tenant-a");
      var pubB = sp.GetKeyedService<IMessagingPublisher>("tenant-b");

      var senderA = sp.GetKeyedService<IMessagingSender>("tenant-a");
      var senderB = sp.GetKeyedService<IMessagingSender>("tenant-b");

      var txA = sp.GetKeyedService<IMessagingTransaction>("tenant-a");
      var txB = sp.GetKeyedService<IMessagingTransaction>("tenant-b");

      var factoryA = sp.GetKeyedService<IKafkaConsumerFactory>("tenant-a");
      var factoryB = sp.GetKeyedService<IKafkaConsumerFactory>("tenant-b");

      var consumerA = sp.GetKeyedService<IConsumer<string, string>>("tenant-a");
      var consumerB = sp.GetKeyedService<IConsumer<string, string>>("tenant-b");

      pubA.Should().NotBeNull();
      pubB.Should().NotBeNull();
      senderA.Should().NotBeNull();
      senderB.Should().NotBeNull();
      txA.Should().NotBeNull();
      txB.Should().NotBeNull();
      factoryA.Should().NotBeNull();
      factoryB.Should().NotBeNull();
      consumerA.Should().NotBeNull();
      consumerB.Should().NotBeNull();

      pubA.Should().NotBeSameAs(pubB);
      senderA.Should().NotBeSameAs(senderB);
      txA.Should().NotBeSameAs(txB);
    }

    [Fact]
    public void ModelProvider_ShouldRespectSingletonLifetime()
    {
      var services = new ServiceCollection();
      services.AddSingleton<IModelProvider, ModelProvider>();
      services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton);

      var descriptor = services.Single(s => s.ServiceType == typeof(IModelProvider));
      descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
  }
}