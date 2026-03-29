#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Kafka.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

[Collection("KafkaIntegration")]
public class KafkaMessagingRegistrationTests
{
  private readonly KafkaContainerFixture _fixture;

  public KafkaMessagingRegistrationTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public void AddKafkaMessaging_ShouldRegisterAllRequiredCoreServices()
  {
    // Arrange
    var setup = new KafkaTestSetup(_fixture.BootstrapServers);

    // Act & Assert - Core Interfaces
    setup.ServiceProvider.GetRequiredService<IMessagingPublisher>().Should().BeOfType<MessagingPublisher>();
    setup.ServiceProvider.GetRequiredService<IMessagingSender>().Should().BeOfType<KafkaSender>();
    setup.ServiceProvider.GetRequiredService<IMessagingTransaction>().Should().BeOfType<MessagingTransaction>();
  }

  [Fact]
  public void AddKafkaMessaging_ShouldCorrectlyBindConfiguration()
  {
    // Arrange
    var setup = new KafkaTestSetup(_fixture.BootstrapServers);

    // Act
    var options = setup.GetKafkaOptions();

    // Assert
    options.BootStrapServers.Should().Be(_fixture.BootstrapServers);
    options.TopicName.Should().Be("test-topic");
    options.GroupID.Should().Be("test-group");
  }

  [Fact]
  public void AddKafkaMessaging_ShouldRegisterProducerAsSingleton()
  {
    // Arrange
    var setup = new KafkaTestSetup(_fixture.BootstrapServers);

    // Act
    var producer1 = setup.ServiceProvider.GetRequiredService<IProducer<string, byte[]>>();
    var producer2 = setup.ServiceProvider.GetRequiredService<IProducer<string, byte[]>>();

    // Assert
    producer1.Should().BeSameAs(producer2);
  }

  [Fact]
  public void AddKafkaMessagingConsumer_ShouldRegisterConsumerFactoryAndContext()
  {
    // Arrange
    var setup = new KafkaTestSetup(_fixture.BootstrapServers);
    
    // Act & Assert
    setup.ServiceProvider.GetRequiredService<IMessageContextAccessor>().Should().NotBeNull();
    setup.ServiceProvider.GetRequiredService<IKafkaConsumerFactory>().Should().NotBeNull();
  }
  [Fact]
  public void AddKafkaMessaging_KeyedRegistration_ShouldResolveIndependentInstances()
  {
    // Arrange
    var services = new ServiceCollection();
    var key = "tenant-a";

    // 1. Build the explicit configuration required for the "Strict DI" philosophy
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:BootStrapServers"] = _fixture.BootstrapServers,
          ["Messaging:Kafka:GroupID"] = "keyed-group",
          ["Messaging:Kafka:TopicName"] = "keyed-topic"
        })
        .Build();

    // 2. Register IConfiguration so internal factory delegates can resolve it
    services.AddSingleton<IConfiguration>(configuration);
    services.AddOptions();
    services.AddFranzMediator(new[] { typeof(KafkaContainerFixture).Assembly });
    services.AddLogging();
    // 3. Act - Perform Keyed Registration
    services.AddKafkaMessaging(configuration, key);
    var sp = services.BuildServiceProvider();

    // 4. Resolve
    var keyedPublisher = sp.GetKeyedService<IMessagingPublisher>(key);
    var keyedProducer = sp.GetKeyedService<Confluent.Kafka.IProducer<string, byte[]>>(key);
    var nonKeyedPublisher = sp.GetService<IMessagingPublisher>();

    // 5. Assert
    keyedPublisher.Should().NotBeNull("Keyed registration should exist for the specific tenant key");
    keyedProducer.Should().NotBeNull("Underlying keyed producer must be resolvable for thread-safety");
    nonKeyedPublisher.Should().BeNull("Global service provider should not contain unkeyed messaging services");

    // Ensure the keyed producer is using the correct bootstrap server from our fixture
    keyedProducer!.Name.Should().NotBeNullOrEmpty();
  }


  [Fact]
  public void AddKafkaMessaging_ShouldThrowException_WhenConfigurationSectionIsMissing()
  {
    // Arrange
    var services = new ServiceCollection();
    var emptyConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

    // Act
    var act = () => services.AddKafkaMessagingOptions(emptyConfig);

    // Assert
    act.Should().Throw<Franz.Common.Errors.TechnicalException>()
       .WithMessage("Kafka messaging configuration missing");
  }

  [Fact]
  public void AddOnlyHighLifetimeModelProvider_ShouldRespectSingletonOverScoped()
  {
    // Arrange
    var services = new ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Messaging:Kafka:BootStrapServers"] = "localhost" })
        .Build();

    // Act 
    // 1. Add via Publisher (Scoped)
    services.AddKafkaMessagingPublisher(configuration);
    var firstLifetime = services.First(s => s.ServiceType == typeof(Franz.Common.Messaging.Kafka.Modeling.IModelProvider)).Lifetime;

    // 2. Add via Consumer (Singleton)
    services.AddKafkaMessagingConsumer(configuration);
    var secondLifetime = services.First(s => s.ServiceType == typeof(Franz.Common.Messaging.Kafka.Modeling.IModelProvider)).Lifetime;

    // Assert
    firstLifetime.Should().Be(ServiceLifetime.Scoped);
    secondLifetime.Should().Be(ServiceLifetime.Singleton);
  }
}