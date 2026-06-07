#nullable enable
using Confluent.Kafka;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
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
  // =========================================================
  // SYSTEM MODE — one topic per service (existing behaviour, unchanged)
  // =========================================================

  public static IServiceCollection AddKafkaMessaging(
      this IServiceCollection services,
      IConfiguration configuration,
      object? serviceKey = null)
  {
    services
        .AddKafkaMessagingOptions(configuration)
        .AddKafkaCore(configuration, serviceKey, perEvent: false)
        .AddKafkaProducerLayer(serviceKey, sharedInitializer: false)
        .AddKafkaMessagingConsumer(serviceKey);

    return services;
  }

  // =========================================================
  // EVENT MODE — one topic per aggregate event type
  //
  // Core infra (admin, serializer, factories, initializer) registers once
  // without a service key — shared across all event registrations.
  //
  // Producer + consumer register per event type using the event type name
  // as the service key. The shared initializer is resolved without a key
  // inside MessagingPublisher construction.
  //
  // AddFranzMediator MUST be called before this method so that
  // DiscoverHandledEventTypes() finds registered handlers.
  // =========================================================

  public static IServiceCollection AddEventBasedKafkaMessaging(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    services
        .AddKafkaMessagingOptions(configuration)
        .AddKafkaCore(configuration, serviceKey: null, perEvent: true);

    var eventTypes = DiscoverHandledEventTypes();

    foreach (var eventType in eventTypes)
    {
      var serviceKey = eventType.Name;

      services
          // sharedInitializer: true — initializer is unkeyed, shared across all events
          .AddKafkaProducerLayer(serviceKey, sharedInitializer: true)
          .AddKafkaMessagingConsumer(serviceKey);
    }

    return services;
  }

  // =========================================================
  // OPTIONS
  // =========================================================

  public static IServiceCollection AddKafkaMessagingOptions(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    services.AddOptions<KafkaMessagingOptions>()
        .BindConfiguration(KafkaMessagingOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return services;
  }

  // =========================================================
  // CORE INFRA (registers once regardless of mode)
  // =========================================================

  private static IServiceCollection AddKafkaCore(
      this IServiceCollection services,
      IConfiguration configuration,
      object? serviceKey,
      bool perEvent)
  {
    services.AddNoDuplicateScoped<IAssemblyAccessor, AssemblyAccessorWrapper>();

    services.AddKafkaProducer(configuration, serviceKey);
    services.AddKafkaAdmin(configuration, serviceKey, perEvent);

    return services
        .AddDefaultMessageSerializer()
        .AddMessagingFactories()
        .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped)
        .AddNoDuplicateSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>()
        .AddNoDuplicateSingleton<IConnectionProvider, ConnectionProvider>();
  }

  // =========================================================
  // PRODUCER CONFIG
  // =========================================================

  private static IServiceCollection AddKafkaProducer(
      this IServiceCollection services,
      IConfiguration _,
      object? serviceKey)
  {
    Func<IServiceProvider, ProducerConfig> configBuilder = sp =>
    {
      var o = sp.GetRequiredService<IOptions<KafkaMessagingOptions>>().Value;

      return new ProducerConfig
      {
        BootstrapServers = o.BootstrapServers,
        ClientId = o.ClientId,
        SecurityProtocol = MapSecurityProtocol(o.Security.SecurityProtocol),
        SaslMechanism = MapSaslMechanism(o.Security.SaslMechanism),
        SaslUsername = o.Security.SaslUsername,
        SaslPassword = o.Security.SaslPassword,
        Acks = MapAcks(o.Producer.Acks),
        EnableIdempotence = o.Producer.EnableIdempotence,
        MessageMaxBytes = o.Producer.MessageMaxBytes,
        LingerMs = o.Producer.LingerMs,
        BatchSize = o.Producer.BatchSize,
        CompressionType = MapCompression(o.Producer.CompressionType),
        MessageSendMaxRetries = o.Producer.MessageSendMaxRetries,
        RetryBackoffMs = o.Producer.RetryBackoffMs,
      };
    };

    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IProducer<string, byte[]>>(serviceKey, (sp, _) =>
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

  // =========================================================
  // ADMIN + INITIALIZER
  // perEvent flag passed into KafkaMessagingInitializer so it
  // knows which topic derivation strategy to use at runtime.
  // =========================================================

  private static IServiceCollection AddKafkaAdmin(
      this IServiceCollection services,
      IConfiguration _,
      object? serviceKey,
      bool perEvent)
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
        SaslPassword = o.Security.SaslPassword,
      };
    };

    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IAdminClient>(serviceKey, (sp, _) =>
          new AdminClientBuilder(configBuilder(sp)).Build());

      services.AddKeyedScoped<IMessagingInitializer>(serviceKey, (sp, key) =>
      {
        var admin = sp.GetRequiredKeyedService<IAdminClient>(key);
        return ActivatorUtilities.CreateInstance<KafkaMessagingInitializer>(
            sp, admin, perEvent);
      });
    }
    else
    {
      services.AddNoDuplicateSingleton<IAdminClient>(sp =>
          new AdminClientBuilder(configBuilder(sp)).Build());

      // Unkeyed — shared across all event mode registrations
      services.AddNoDuplicateScoped<IMessagingInitializer>(sp =>
          ActivatorUtilities.CreateInstance<KafkaMessagingInitializer>(
              sp,
              sp.GetRequiredService<IAdminClient>(),
              perEvent));
    }

    return services;
  }

  // =========================================================
  // PRODUCER LAYER
  //
  // sharedInitializer: false (system mode)
  //   → IMessagingInitializer resolved by the same serviceKey
  //     as the producer/sender (keyed registration per tenant/instance)
  //
  // sharedInitializer: true (event mode)
  //   → IMessagingInitializer resolved without a key
  //     (one shared initializer handles all event topics)
  // =========================================================

  private static IServiceCollection AddKafkaProducerLayer(
      this IServiceCollection services,
      object? serviceKey,
      bool sharedInitializer = false)
  {
    if (serviceKey != null)
    {
      services.AddKeyedScoped<IMessagingSender, KafkaSender>(serviceKey);

      services.AddKeyedScoped<IMessagingPublisher>(serviceKey, (sp, key) =>
          new MessagingPublisher(
              sharedInitializer
                  // Event mode — shared unkeyed initializer
                  ? sp.GetRequiredService<IMessagingInitializer>()
                  // System mode — keyed initializer per instance
                  : sp.GetRequiredKeyedService<IMessagingInitializer>(key),
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

  // =========================================================
  // CONSUMER LAYER (unchanged)
  // =========================================================

  private static IServiceCollection AddKafkaMessagingConsumer(
      this IServiceCollection services,
      object? serviceKey)
  {
    if (serviceKey != null)
    {
      services.AddKeyedScoped<MessageContextAccessor>(serviceKey);
      services.AddKeyedScoped<IMessageContextAccessor>(serviceKey,
          (sp, key) => sp.GetRequiredKeyedService<MessageContextAccessor>(key));
      services.AddKeyedSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>(serviceKey);
      services.AddKeyedSingleton<IConsumer<string, string>>(serviceKey,
          (sp, key) => sp.GetRequiredKeyedService<IKafkaConsumerFactory>(key).Build());
    }
    else
    {
      services.AddNoDuplicateScoped<MessageContextAccessor>();
      services.AddNoDuplicateScoped<IMessageContextAccessor>(
          sp => sp.GetRequiredService<MessageContextAccessor>());
      services.AddNoDuplicateSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();
      services.AddNoDuplicateSingleton<IConsumer<string, string>>(
          sp => sp.GetRequiredService<IKafkaConsumerFactory>().Build());
    }

    return services;
  }

  // =========================================================
  // EVENT TYPE DISCOVERY
  // =========================================================

  private static IEnumerable<Type> DiscoverHandledEventTypes()
      => AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a =>
          {
            try { return a.ExportedTypes; }
            catch { return Array.Empty<Type>(); }
          })
          .SelectMany(t => t.GetInterfaces())
          .Where(i => i.IsGenericType &&
                      i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
          .SelectMany(i => i.GenericTypeArguments)
          .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t))
          .Distinct();

  // =========================================================
  // MODEL PROVIDER (unchanged)
  // =========================================================

  public static IServiceCollection AddOnlyHighLifetimeModelProvider(
      this IServiceCollection services,
      ServiceLifetime lifetime)
  {
    var existing = services.SingleOrDefault(x => x.ServiceType == typeof(IModelProvider));

    if (existing != null)
    {
      if (existing.Lifetime != lifetime && lifetime == ServiceLifetime.Singleton)
      {
        services.Remove(existing);
        services.Add(new ServiceDescriptor(
            typeof(IModelProvider), typeof(ModelProvider), lifetime));
      }
    }
    else
    {
      services.Add(new ServiceDescriptor(
          typeof(IModelProvider), typeof(ModelProvider), lifetime));
    }

    return services;
  }

  // =========================================================
  // MAPPERS (unchanged)
  // =========================================================

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
}