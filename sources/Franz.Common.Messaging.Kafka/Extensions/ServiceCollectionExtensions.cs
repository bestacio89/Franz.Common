#nullable enable
using Confluent.Kafka;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Errors;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration, object? serviceKey = null)
  {
    return services
        .AddKafkaMessagingPublisher(configuration, serviceKey)
        .AddKafkaMessagingSender(configuration, serviceKey)
        .AddKafkaMessagingConsumer(configuration, serviceKey);
  }

  public static IServiceCollection AddKafkaMessagingSender(this IServiceCollection services, IConfiguration? configuration = null, object? serviceKey = null)
  {
    if (serviceKey != null)
      services.AddKeyedScoped<IMessagingSender, KafkaSender>(serviceKey);
    else
      services.AddNoDuplicateScoped<IMessagingSender, KafkaSender>();

    return services.AddCommonMessagingProducer(configuration, serviceKey);
  }

  public static IServiceCollection AddKafkaMessagingPublisher(this IServiceCollection services, IConfiguration configuration, object? serviceKey = null)
  {
    if (serviceKey != null)
    {
      services.AddKeyedScoped<IMessagingPublisher>(serviceKey, (sp, key) =>
      {
        // Explicitly bridge Keyed and Global dependencies
        return new MessagingPublisher(
            sp.GetRequiredKeyedService<IMessagingInitializer>(key),
            sp.GetRequiredService<IMessageFactory>(), // Resolve from Global
            sp.GetRequiredService<IDispatcher>(),     // Resolve from Global
            sp.GetRequiredKeyedService<IMessagingSender>(key)
        );
      });
    }
    else
    {
      services.AddNoDuplicateScoped<IMessagingPublisher, MessagingPublisher>();
    }

    return services.AddCommonMessagingProducer(configuration, serviceKey);
  }

  private static IServiceCollection AddCommonMessagingProducer(this IServiceCollection services, IConfiguration? configuration, object? serviceKey = null)
  {
    services.AddNoDuplicateScoped<IAssemblyAccessor, AssemblyAccessorWrapper>();

    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IProducer<string, byte[]>>(serviceKey, (sp, key) =>
      {
        var options = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
        var config = new ProducerConfig { BootstrapServers = options.BootStrapServers, Acks = Acks.All, EnableIdempotence = true };
        return new ProducerBuilder<string, byte[]>(config)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.ByteArray)
            .Build();
      });

      services.AddKeyedScoped<IMessagingTransaction, MessagingTransaction>(serviceKey);
      services.AddKeyedScoped<IMessageHandler, MessageBuilderDelegatingHandler>(serviceKey);
    }
    else
    {
      services.AddNoDuplicateSingleton<IProducer<string, byte[]>>(sp =>
      {
        var options = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
        var config = new ProducerConfig { BootstrapServers = options.BootStrapServers, Acks = Acks.All, EnableIdempotence = true };
        return new ProducerBuilder<string, byte[]>(config)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.ByteArray)
            .Build();
      });

      services.AddNoDuplicateScoped<IMessagingTransaction, MessagingTransaction>();
      services.AddNoDuplicateScoped<IMessageHandler, MessageBuilderDelegatingHandler>();
    }

    return services.AddKafkaMessagingConfiguration(configuration, serviceKey);
  }

  public static IServiceCollection AddKafkaMessagingOptions(this IServiceCollection services, IConfiguration? configuration)
  {
    var section = configuration.GetSection("Messaging:Kafka");
    if (!section.Exists())
      throw new TechnicalException("Kafka messaging configuration missing");

    services.AddOptions<KafkaMessagingOptions>().Bind(section);
    return services;
  }
  public static IServiceCollection AddKafkaMessagingConfiguration(this IServiceCollection services, IConfiguration? configuration, object? serviceKey = null)
  {
    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IAdminClient>(serviceKey, (sp, key) =>
      {
        var options = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
        return new AdminClientBuilder(new AdminClientConfig { BootstrapServers = options.BootStrapServers }).Build();
      });

      // FIX: Explicitly bridge the keyed AdminClient to the Initializer
      services.AddKeyedScoped<IMessagingInitializer>(serviceKey, (sp, key) =>
      {
        var adminClient = sp.GetRequiredKeyedService<IAdminClient>(key);
        // Use ActivatorUtilities to satisfy other dependencies (ILogger, etc.) while forcing our keyed AdminClient
        return ActivatorUtilities.CreateInstance<KafkaMessagingInitializer>(sp, adminClient);
      });
    }
    else
    {
      services.AddNoDuplicateSingleton<IAdminClient>(sp =>
      {
        var options = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;
        return new AdminClientBuilder(new AdminClientConfig { BootstrapServers = options.BootStrapServers }).Build();
      });

      services.AddNoDuplicateScoped<IMessagingInitializer, KafkaMessagingInitializer>();
    }

    return services
        .AddDefaultMessageSerializer()
        .AddMessagingFactories()
        .AddKafkaMessagingOptions(configuration)
        .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped)
        .AddNoDuplicateSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>()
        .AddNoDuplicateSingleton<IConnectionProvider, ConnectionProvider>();
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

  public static IServiceCollection AddKafkaMessagingConsumer(this IServiceCollection services, IConfiguration configuration, object? serviceKey = null)
  {
    services
        .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton)
        .AddKafkaMessagingConfiguration(configuration, serviceKey);

    if (serviceKey != null)
    {
      services.AddKeyedScoped<MessageContextAccessor>(serviceKey);
      services.AddKeyedScoped<IMessageContextAccessor>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<MessageContextAccessor>(key));
      services.AddKeyedSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>(serviceKey);
      services.AddKeyedSingleton<IConsumer<string, string>>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<IKafkaConsumerFactory>(key).Build());
    }
    else
    {
      services.AddNoDuplicateScoped<MessageContextAccessor>();
      services.AddNoDuplicateScoped<IMessageContextAccessor>(sp => sp.GetRequiredService<MessageContextAccessor>());
      services.AddNoDuplicateSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();
      services.AddNoDuplicateSingleton<IConsumer<string, string>>(sp => sp.GetRequiredService<IKafkaConsumerFactory>().Build());
    }

    return services;
  }
}