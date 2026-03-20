using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Contexting;

namespace Franz.Common.Messaging.Kafka.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
  private static IConfiguration CreateConfiguration()
  {
    var dict = new Dictionary<string, string?>
    {
      ["Messaging:BootStrapServers"] = "localhost:9092",
      ["Messaging:GroupID"] = "test-group"
    };

    return new ConfigurationBuilder()
        .AddInMemoryCollection(dict!)
        .Build();
  }

  [Fact]
  public void AddKafkaMessagingConfiguration_Should_Register_Core_Services()
  {
    var services = new ServiceCollection();
    var configuration = CreateConfiguration();

    services.AddKafkaMessagingConfiguration(configuration);

    var provider = services.BuildServiceProvider();

    provider.GetService<IConnectionFactoryProvider>().Should().NotBeNull();
    provider.GetService<IConnectionProvider>().Should().NotBeNull();
    provider.GetService<IMessagingInitializer>().Should().NotBeNull();
  }

  [Fact]
  public void AddKafkaMessagingPublisher_Should_Register_Publisher_And_Producer()
  {
    var services = new ServiceCollection();
    var configuration = CreateConfiguration();

    services.AddKafkaMessagingPublisher(configuration);

    var provider = services.BuildServiceProvider();

    provider.GetService<IMessagingPublisher>().Should().NotBeNull();
    provider.GetService<IProducer<string, byte[]>>().Should().NotBeNull();
    provider.GetService<IAdminClient>().Should().NotBeNull();
  }

  [Fact]
  public void AddKafkaMessagingSender_Should_Register_Sender()
  {
    var services = new ServiceCollection();
    var configuration = CreateConfiguration();

    services.AddKafkaMessagingSender(configuration);

    var provider = services.BuildServiceProvider();

    provider.GetService<IMessagingSender>().Should().NotBeNull();
    provider.GetService<IProducer<string, byte[]>>().Should().NotBeNull();
  }

  [Fact]
  public void AddKafkaMessagingConsumer_Should_Register_Consumer_And_Factory()
  {
    var services = new ServiceCollection();
    var configuration = CreateConfiguration();

    services.AddKafkaMessagingConsumer(configuration);

    var provider = services.BuildServiceProvider();

    provider.GetService<IConsumer<string, string>>().Should().NotBeNull();
    provider.GetService<IKafkaConsumerFactory>().Should().NotBeNull();
    provider.GetService<IMessageContextAccessor>().Should().NotBeNull();
  }

  [Fact]
  public void AddKafkaMessaging_Should_Register_All_Components()
  {
    var services = new ServiceCollection();
    var configuration = CreateConfiguration();

    services.AddKafkaMessaging(configuration);

    var provider = services.BuildServiceProvider();

    provider.GetService<IMessagingSender>().Should().NotBeNull();
    provider.GetService<IMessagingPublisher>().Should().NotBeNull();
    provider.GetService<IProducer<string, byte[]>>().Should().NotBeNull();
    provider.GetService<IConsumer<string, string>>().Should().NotBeNull();
    provider.GetService<IConnectionProvider>().Should().NotBeNull();
    provider.GetService<IConnectionFactoryProvider>().Should().NotBeNull();
  }

  [Fact]
  public void AddOnlyHighLifetimeModelProvider_Should_Upgrade_To_Singleton()
  {
    var services = new ServiceCollection();

    services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped);
    services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton);

    var descriptor = services
        .First(x => x.ServiceType == typeof(IModelProvider));

    descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  [Fact]
  public void AddOnlyHighLifetimeModelProvider_Should_Not_Downgrade_From_Singleton()
  {
    var services = new ServiceCollection();

    services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton);
    services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped);

    var descriptor = services
        .First(x => x.ServiceType == typeof(IModelProvider));

    descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }
}