#nullable enable
using Confluent.Kafka;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddKafkaMessagingPublisher(configuration)
      .AddKafkaMessagingSender(configuration)
      .AddKafkaMessagingConsumer(configuration);

    return services;
  }

  public static IServiceCollection AddKafkaMessagingSender(this IServiceCollection services, IConfiguration? configuration = null)
  {
    services
      .AddNoDuplicateScoped<IMessagingSender, MessagingSender>()
      .AddCommonMessagingProducer(configuration);

    return services;
  }

  public static IServiceCollection AddKafkaMessagingPublisher(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddNoDuplicateScoped<IMessagingPublisher, MessagingPublisher>()
      .AddCommonMessagingProducer(configuration);

    return services;
  }

  private static IServiceCollection AddCommonMessagingProducer(
    this IServiceCollection services,
    IConfiguration? configuration)
  {
    
    services.AddSingleton<IAdminClient>(sp =>
    {
      var config = new AdminClientConfig
      {
        BootstrapServers = configuration["Messaging:BootStrapServers"]
      };

      return new AdminClientBuilder(config).Build();
    });


    services
      .AddKafkaMessagingConfiguration(configuration)


      // Kafka producer (Confluent-native)
      .AddNoDuplicateSingleton<IProducer<string, byte[]>>(sp =>
      {
        var options = sp.GetRequiredService<IOptions<MessagingOptions>>().Value;

        var config = new ProducerConfig
        {
          BootstrapServers = options.BootStrapServers,
          Acks = Acks.All,
          EnableIdempotence = true
        };

        return new ProducerBuilder<string, byte[]>(config)
          .SetKeySerializer(Serializers.Utf8)
          .SetValueSerializer(Serializers.ByteArray)
          .Build();
      })

      // Franz messaging core
      .AddNoDuplicateScoped<IMessagingTransaction, MessagingTransaction>()
      .AddNoDuplicateScoped<IMessageFactory, MessageFactory>()
      .AddNoDuplicateScoped<IMessageHandler, MessageBuilderDelegatingHandler>();

    return services;
  }


  public static IServiceCollection AddKafkaMessagingConfiguration(this IServiceCollection services, IConfiguration? configuration)
  {
    services
      .AddMessagingOptions(configuration)
      .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped)
      .AddNoDuplicateSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>()
      .AddNoDuplicateSingleton<IConnectionProvider, ConnectionProvider>()
      .AddNoDuplicateScoped<IMessagingInitializer, MessagingInitializer>();

    return services;
  }

  public static IServiceCollection AddOnlyHighLifetimeModelProvider(this IServiceCollection services, ServiceLifetime serviceLifetime)
  {
    var serviceTypeModelProvider = typeof(IModelProvider);
    var serviceDescriptor = new ServiceDescriptor(serviceTypeModelProvider, typeof(ModelProvider), serviceLifetime);

    var serviceModelProvider = services.SingleOrDefault(service => service.ServiceType == serviceTypeModelProvider);

    if (serviceModelProvider != null)
    {
      if (serviceModelProvider.Lifetime != serviceLifetime && serviceLifetime == ServiceLifetime.Singleton)
      {
        services.Remove(serviceModelProvider);
        services.Add(serviceDescriptor);
      }
    }
    else
    {
      services.Add(serviceDescriptor);
    }

    return services;
  }

  public static IServiceCollection AddKafkaMessagingConsumer(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services
        // Messaging + modeling
        .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton)
        .AddKafkaMessagingConfiguration(configuration)

        // Message context
        .AddNoDuplicateScoped<MessageContextAccessor>()
        .AddNoDuplicateScoped<IMessageContextAccessor>(
            sp => sp.GetRequiredService<MessageContextAccessor>())

        // Kafka consumer factory (builds the real Confluent consumer)
        .AddNoDuplicateSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>()

        // The ACTUAL Kafka consumer (Confluent owns the abstraction)
        .AddNoDuplicateSingleton<IConsumer<string, string>>(sp =>
        {
          var factory = sp.GetRequiredService<IKafkaConsumerFactory>();
          return factory.Build();
        }
        );
       return services;
  }
}
