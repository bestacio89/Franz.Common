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
using System.Configuration;
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

      var modelProvider = sp.GetRequiredService<IModelProvider>();
      modelProvider.Should().BeOfType<ModelProvider>();
    }

    [Fact]
    public void Options_Should_Bind_Correctly_FromFixture()
    {
      var sp = _fixture.BuildServiceProvider();
      var options = _fixture.GetOptions(sp);

      options.BootstrapServers.Should().Be(_fixture.BootstrapServers);
      options.GroupId.Should().Be("integration-test-group");
      options.TopicName.Should().Be("integration-test");

      // Consumer & Producer options are set
      options.Consumer.EnableAutoCommit.Should().BeFalse();
      options.Producer.EnableIdempotence.Should().BeTrue();
    }

    [Fact]
    public void Producer_ShouldBeSingleton()
    {
      var sp = _fixture.BuildServiceProvider();
      var producer1 = sp.GetRequiredService<IProducer<string, byte[]>>();
      var producer2 = sp.GetRequiredService<IProducer<string, byte[]>>();
      producer1.Should().BeSameAs(producer2);
    }

    [Fact]
    public void ConsumerFactory_ShouldBeSingleton()
    {
      var sp = _fixture.BuildServiceProvider();
      var factory1 = sp.GetRequiredService<IKafkaConsumerFactory>();
      var factory2 = sp.GetRequiredService<IKafkaConsumerFactory>();
      factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void KeyedServices_ShouldResolveCorrectly()
    {
      var key = "tenant-a";
      var sp = _fixture.BuildServiceProvider(services =>
      {
        services.AddKafkaMessaging(_fixture.Configuration, key);
      });

      var keyedPublisher = sp.GetKeyedService<IMessagingPublisher>(key);
      var keyedSender = sp.GetKeyedService<IMessagingSender>(key);
      var keyedTransaction = sp.GetKeyedService<IMessagingTransaction>(key);
      var keyedConsumerFactory = sp.GetKeyedService<IKafkaConsumerFactory>(key);
      var keyedConsumer = sp.GetKeyedService<IConsumer<string, string>>(key);

      keyedPublisher.Should().NotBeNull();
      keyedSender.Should().NotBeNull();
      keyedTransaction.Should().NotBeNull();
      keyedConsumerFactory.Should().NotBeNull();
      keyedConsumer.Should().NotBeNull();
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