#nullable enable
using Confluent.Kafka;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Configuration;
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
    services
      .AddKafkaMessagingOptions(configuration)
      .AddKafkaCore(configuration, serviceKey)
      .AddKafkaProducerLayer(serviceKey)
      .AddKafkaMessagingConsumer(serviceKey);

    return services;
  }

  // =========================
  // 🔧 OPTIONS
  // =========================

  public static IServiceCollection AddKafkaMessagingOptions(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOptions<KafkaMessagingOptions>()
        .BindConfiguration(KafkaMessagingOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return services;
  }

  // =========================
  // ⚙️ CORE INFRA (ONCE)
  // =========================

  private static IServiceCollection AddKafkaCore(this IServiceCollection services, IConfiguration configuration, object? serviceKey)
  {
    services.AddNoDuplicateScoped<IAssemblyAccessor, AssemblyAccessorWrapper>();

    // 🔹 Producer
    services.AddKafkaProducer(configuration, serviceKey);

    // 🔹 Admin
    services.AddKafkaAdmin(configuration, serviceKey);

    return services
        .AddDefaultMessageSerializer()
        .AddMessagingFactories()
        .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped)
        .AddNoDuplicateSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>()
        .AddNoDuplicateSingleton<IConnectionProvider, ConnectionProvider>();
  }

  // =========================
  // 📤 PRODUCER CONFIG
  // =========================

  private static IServiceCollection AddKafkaProducer(this IServiceCollection services, IConfiguration _, object? serviceKey)
  {
    Func<IServiceProvider, ProducerConfig> configBuilder = sp =>
    {
      var o = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;

      return new ProducerConfig
      {
        BootstrapServers = o.BootstrapServers,
        ClientId = o.ClientId,

        // Security
        SecurityProtocol = MapSecurityProtocol(o.Security.SecurityProtocol),
        SaslMechanism = MapSaslMechanism(o.Security.SaslMechanism),
        SaslUsername = o.Security.SaslUsername,
        SaslPassword = o.Security.SaslPassword,

        // Producer
        Acks = MapAcks(o.Producer.Acks),
        EnableIdempotence = o.Producer.EnableIdempotence,
        MessageMaxBytes = o.Producer.MessageMaxBytes,
        LingerMs = o.Producer.LingerMs,
        BatchSize = o.Producer.BatchSize,
        CompressionType = MapCompression(o.Producer.CompressionType),

        // Reliability
        MessageSendMaxRetries = o.Producer.MessageSendMaxRetries,
        RetryBackoffMs = o.Producer.RetryBackoffMs,
       };
    };

    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IProducer<string, byte[]>>(serviceKey, (sp, key) =>
        new ProducerBuilder<string, byte[]>(configBuilder(sp))
          .SetKeySerializer(Serializers.Utf8)
          .SetValueSerializer(Serializers.ByteArray)
          .Build());
    }
    else
    {
      services.AddNoDuplicateSingleton<IProducer<string, byte[]>>(sp =>
        new ProducerBuilder<string, byte[]>(configBuilder(sp))
          .SetKeySerializer(Serializers.Utf8)
          .SetValueSerializer(Serializers.ByteArray)
          .Build());
    }

    return services;
  }

  // =========================
  // 🧾 ADMIN CONFIG
  // =========================

  private static IServiceCollection AddKafkaAdmin(this IServiceCollection services, IConfiguration _, object? serviceKey)
  {
    Func<IServiceProvider, AdminClientConfig> configBuilder = sp =>
    {
      var o = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;

      return new AdminClientConfig
      {
        BootstrapServers = o.BootstrapServers,
        ClientId = o.ClientId,
        SecurityProtocol = MapSecurityProtocol(o.Security.SecurityProtocol),
        SaslMechanism = MapSaslMechanism(o.Security.SaslMechanism),
        SaslUsername = o.Security.SaslUsername,
        SaslPassword = o.Security.SaslPassword
      };
    };

    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IAdminClient>(serviceKey, (sp, key) =>
        new AdminClientBuilder(configBuilder(sp)).Build());

      services.AddKeyedScoped<IMessagingInitializer>(serviceKey, (sp, key) =>
      {
        var admin = sp.GetRequiredKeyedService<IAdminClient>(key);
        return ActivatorUtilities.CreateInstance<KafkaMessagingInitializer>(sp, admin);
      });
    }
    else
    {
      services.AddNoDuplicateSingleton<IAdminClient>(sp =>
        new AdminClientBuilder(configBuilder(sp)).Build());

      services.AddNoDuplicateScoped<IMessagingInitializer, KafkaMessagingInitializer>();
    }

    return services;
  }

  // =========================
  // 📤 PRODUCER LAYER
  // =========================

  private static IServiceCollection AddKafkaProducerLayer(this IServiceCollection services, object? serviceKey)
  {
    if (serviceKey != null)
    {
      services.AddKeyedScoped<IMessagingSender, KafkaSender>(serviceKey);

      services.AddKeyedScoped<IMessagingPublisher>(serviceKey, (sp, key) =>
        new MessagingPublisher(
          sp.GetRequiredKeyedService<IMessagingInitializer>(key),
          sp.GetRequiredService<IMessageFactory>(),
          sp.GetRequiredService<IDispatcher>(),
          sp.GetRequiredKeyedService<IMessagingSender>(key)
        ));

      services.AddKeyedScoped<IMessagingTransaction, MessagingTransaction>(serviceKey);
      services.AddKeyedScoped<IMessageHandler, MessageBuilderDelegatingHandler>(serviceKey);
    }
    else
    {
      services.AddNoDuplicateScoped<IMessagingSender, KafkaSender>();
      services.AddNoDuplicateScoped<IMessagingPublisher, MessagingPublisher>();
      services.AddNoDuplicateScoped<IMessagingTransaction, MessagingTransaction>();
      services.AddNoDuplicateScoped<IMessageHandler, MessageBuilderDelegatingHandler>();
    }

    return services;
  }

  // =========================
  // 📥 CONSUMER LAYER
  // =========================

  private static IServiceCollection AddKafkaMessagingConsumer(this IServiceCollection services, object? serviceKey)
  {
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

  // =========================
  // 🔁 MAPPERS
  // =========================

  private static Acks MapAcks(KafkaAcks a) => a switch
  {
    KafkaAcks.None => Acks.None,
    KafkaAcks.Leader => Acks.Leader,
    _ => Acks.All
  };

  private static CompressionType MapCompression(KafkaCompressionType c) => c switch
  {
    KafkaCompressionType.Gzip => CompressionType.Gzip,
    KafkaCompressionType.Lz4 => CompressionType.Lz4,
    KafkaCompressionType.Zstd => CompressionType.Zstd,
    KafkaCompressionType.None => CompressionType.None,
    _ => CompressionType.Snappy
  };

  private static SecurityProtocol MapSecurityProtocol(KafkaSecurityProtocol p) => p switch
  {
    KafkaSecurityProtocol.Ssl => SecurityProtocol.Ssl,
    KafkaSecurityProtocol.SaslPlaintext => SecurityProtocol.SaslPlaintext,
    KafkaSecurityProtocol.SaslSsl => SecurityProtocol.SaslSsl,
    _ => SecurityProtocol.Plaintext
  };

  private static SaslMechanism? MapSaslMechanism(KafkaSaslMechanism? m) => m switch
  {
    KafkaSaslMechanism.Plain => SaslMechanism.Plain,
    KafkaSaslMechanism.ScramSha256 => SaslMechanism.ScramSha256,
    KafkaSaslMechanism.ScramSha512 => SaslMechanism.ScramSha512,
    KafkaSaslMechanism.Gssapi => SaslMechanism.Gssapi,
    KafkaSaslMechanism.OAuthBearer => SaslMechanism.OAuthBearer,
    _ => null
  };

  // =========================
  // 🧠 MODEL PROVIDER
  // =========================

  public static IServiceCollection AddOnlyHighLifetimeModelProvider(this IServiceCollection services, ServiceLifetime lifetime)
  {
    var existing = services.SingleOrDefault(x => x.ServiceType == typeof(IModelProvider));

    if (existing != null)
    {
      if (existing.Lifetime != lifetime && lifetime == ServiceLifetime.Singleton)
      {
        services.Remove(existing);
        services.Add(new ServiceDescriptor(typeof(IModelProvider), typeof(ModelProvider), lifetime));
      }
    }
    else
    {
      services.Add(new ServiceDescriptor(typeof(IModelProvider), typeof(ModelProvider), lifetime));
    }

    return services;
  }
}