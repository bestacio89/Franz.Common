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
using Franz.Common.Messaging.Manifest;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Franz.Common.Messaging.Kafka.Validation;

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
  // the generated manifest supplies the registered handler set.
  // =========================================================

  public static IServiceCollection AddEventBasedKafkaMessaging(
      this IServiceCollection services,
      IConfiguration configuration)
      => AddEventBasedKafkaMessagingCore(
          services,
          configuration,
          DiscoverHandledEventTypes());

  /// <summary>
  /// Registers event-based Kafka messaging for an explicit set of integration event types.
  /// Prefer this overload when the application owns a known event contract set; it avoids
  /// assembly-wide handler discovery during service registration.
  /// </summary>
  public static IServiceCollection AddEventBasedKafkaMessaging(
      this IServiceCollection services,
      IConfiguration configuration,
      params Type[] eventTypes)
  {
    ArgumentNullException.ThrowIfNull(eventTypes);

    if (eventTypes.Any(static type =>
        type is null ||
        !type.IsClass ||
        type.IsAbstract ||
        !typeof(IIntegrationEvent).IsAssignableFrom(type)))
    {
      throw new ArgumentException(
          "Every event type must be a concrete class implementing IIntegrationEvent.",
          nameof(eventTypes));
    }

    var duplicateName = eventTypes
        .GroupBy(static type => type.Name, StringComparer.Ordinal)
        .FirstOrDefault(static group => group.Count() > 1);

    if (duplicateName is not null)
    {
      throw new ArgumentException(
          $"Event types must have unique simple names because event names are used as DI keys. Duplicate: '{duplicateName.Key}'.",
          nameof(eventTypes));
    }

    ValidateConfiguredSubscriptions(eventTypes);
    return AddEventBasedKafkaMessagingCore(
        services,
        configuration,
        eventTypes.Distinct());
  }

  private static IServiceCollection AddEventBasedKafkaMessagingCore(
      IServiceCollection services,
      IConfiguration configuration,
      IEnumerable<Type> eventTypes)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(eventTypes);

    services
        .AddKafkaMessagingOptions(configuration)
        .AddKafkaCore(configuration, serviceKey: null, perEvent: true);

    foreach (var eventType in eventTypes)
    {
      var serviceKey = eventType.Name;

      services
          .AddKafkaProducerLayer(serviceKey, sharedInitializer: true, eventType: eventType)
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
    services.TryAddSingleton<IConfiguration>(configuration);
    services.AddSingleton<IValidateOptions<KafkaMessagingOptions>, KafkaMessagingOptionsValidator>();

    var section = configuration.GetSection(KafkaMessagingOptions.SectionName);
    var provisioningModeIsConfigured = section["ProvisioningMode"] is not null;

    services.AddOptions<KafkaMessagingOptions>()
        .Bind(section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddSingleton<IConfigureOptions<KafkaMessagingOptions>>(serviceProvider =>
        new ConfigureNamedOptions<KafkaMessagingOptions>(Options.DefaultName, options =>
        {
          if (provisioningModeIsConfigured)
            return;

          var environment = serviceProvider.GetService<IHostEnvironment>();
          options.ProvisioningMode = environment?.IsDevelopment() == true
              ? Franz.Common.Messaging.Configuration.ProvisioningMode.Create
              : Franz.Common.Messaging.Configuration.ProvisioningMode.Verify;
        }));

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
    services.AddNoDuplicateSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
    services.AddNoDuplicateSingleton<KafkaTopicNames>();
    services.AddNoDuplicateSingleton<KafkaRebalanceCoordinator>();
    services.TryAddSingleton<IServiceCollection>(services);
    services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KafkaMessagingManifestValidationHostedService>());

    services.AddKafkaProducer(configuration, serviceKey);
    services.AddKafkaAdmin(configuration, serviceKey, perEvent);

    return services
        .AddDefaultMessageSerializer()
        .AddMessagingFactories()
        .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Scoped)
        .AddNoDuplicateSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>()
        .AddNoDuplicateSingleton<IConnectionProvider, ConnectionProvider>()
        .AddNoDuplicateSingleton<IKafkaTransactionProducerFactory, KafkaTransactionProducerFactory>()
        .AddNoDuplicateScoped<KafkaTransactionContext>();
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
        SslCaLocation = o.Security.SslCaLocation,
        SslCertificateLocation = o.Security.SslCertificateLocation,
        SslKeyLocation = o.Security.SslKeyLocation,
        Acks = MapAcks(o.Producer.Acks),
        EnableIdempotence = o.Producer.EnableIdempotence,
        MessageMaxBytes = o.Producer.MessageMaxBytes,
        LingerMs = o.Producer.LingerMs,
        BatchSize = o.Producer.BatchSize,
        CompressionType = MapCompression(o.Producer.CompressionType),
        MessageSendMaxRetries = o.Producer.MessageSendMaxRetries,
        RetryBackoffMs = o.Producer.RetryBackoffMs,
        MessageTimeoutMs = o.Producer.DeliveryTimeoutMs,
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
        SslCaLocation = o.Security.SslCaLocation,
        SslCertificateLocation = o.Security.SslCertificateLocation,
        SslKeyLocation = o.Security.SslKeyLocation,
      };
    };

    if (serviceKey != null)
    {
      services.AddKeyedSingleton<IAdminClient>(serviceKey, (sp, _) =>
          new AdminClientBuilder(configBuilder(sp)).Build());

      services.AddKeyedScoped<IMessagingInitializer>(serviceKey, (sp, key) =>
      {
        var admin = sp.GetRequiredKeyedService<IAdminClient>(key);
        return new KafkaMessagingInitializer(
            admin,
            sp.GetRequiredService<KafkaTopicNames>(),
            sp.GetRequiredService<IOptions<KafkaMessagingOptions>>(),
            perEvent);
      });
    }
    else
    {
      services.AddNoDuplicateSingleton<IAdminClient>(sp =>
          new AdminClientBuilder(configBuilder(sp)).Build());

      // Unkeyed — shared across all event mode registrations
      services.AddNoDuplicateScoped<IMessagingInitializer>(sp =>
          new KafkaMessagingInitializer(
              sp.GetRequiredService<IAdminClient>(),
              sp.GetRequiredService<KafkaTopicNames>(),
              sp.GetRequiredService<IOptions<KafkaMessagingOptions>>(),
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
      bool sharedInitializer = false,
      Type? eventType = null)
  {
    if (serviceKey != null)
    {
      services.AddKeyedScoped<IMessagingSender>(serviceKey, (sp, key) =>
          new KafkaSender(
              sharedInitializer
                  ? sp.GetRequiredService<IProducer<string, byte[]>>()
                  : sp.GetRequiredKeyedService<IProducer<string, byte[]>>(key),
              sp.GetRequiredService<IMessageSerializer>(),
              sp.GetRequiredService<KafkaTopicNames>(),
              sp.GetRequiredService<IOptions<KafkaMessagingOptions>>(),
              sp.GetRequiredService<ILogger<KafkaSender>>(),
              sp.GetRequiredService<KafkaTransactionContext>(),
              eventType is null ? null : TopicNamer.GetTopicName(eventType)));

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

      services.AddKeyedScoped<IMessagingTransaction>(serviceKey, (sp, _) =>
          new MessagingTransaction(
              sp.GetRequiredService<IKafkaTransactionProducerFactory>(),
              sp.GetRequiredService<KafkaTransactionContext>()));
      services.AddKeyedScoped<IMessageHandler, MessageBuilderDelegatingHandler>(serviceKey);
    }
    else
    {
      services.AddNoDuplicateScoped<IMessagingSender, KafkaSender>();
      services.AddNoDuplicateScoped<IMessagingPublisher, MessagingPublisher>();
      services.AddNoDuplicateScoped<IMessagingTransaction>(sp =>
          new MessagingTransaction(
              sp.GetRequiredService<IKafkaTransactionProducerFactory>(),
              sp.GetRequiredService<KafkaTransactionContext>()));
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
      => GetManifestSubscriptions().OrderBy(static type => type.FullName, StringComparer.Ordinal);

  private static void ValidateConfiguredSubscriptions(IEnumerable<Type> configuredTypes)
  {
    var manifestSubscriptions = GetManifestSubscriptions();
    var unhandled = configuredTypes
        .Except(manifestSubscriptions)
        .OrderBy(static type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    if (unhandled.Length > 0)
      throw new InvalidOperationException(
          $"Kafka configured subscriptions do not have generated handlers. Unhandled configured subscriptions: {string.Join(", ", unhandled.Select(static type => type.FullName))}.");
  }

  private static HashSet<Type> GetManifestSubscriptions()
      => MessagingManifestRegistry.GetAll()
          .SelectMany(static manifest => manifest.Subscriptions)
          .Where(static type => typeof(IIntegrationEvent).IsAssignableFrom(type))
          .ToHashSet();

  private static HashSet<Type> GetRegisteredIntegrationEventHandlers(IServiceCollection services)
      => services.Where(static descriptor => descriptor.ServiceType.IsGenericType &&
              descriptor.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
          .Select(static descriptor => descriptor.ServiceType.GenericTypeArguments[0])
          .Where(static type => typeof(IIntegrationEvent).IsAssignableFrom(type)).ToHashSet();

  private static void ValidateManifestSubscriptions(
      IReadOnlySet<Type> registeredHandlers,
      IReadOnlySet<Type> manifestSubscriptions)
  {
    var missingFromManifest = registeredHandlers.Except(manifestSubscriptions).ToArray();
    var missingHandlers = manifestSubscriptions.Except(registeredHandlers).ToArray();
    if (missingFromManifest.Length > 0 || missingHandlers.Length > 0)
      throw new InvalidOperationException(
          $"Generated messaging manifest and registered integration event handlers disagree. Missing from manifest: {string.Join(", ", missingFromManifest.Select(static type => type.FullName))}. Missing registered handlers: {string.Join(", ", missingHandlers.Select(static type => type.FullName))}.");
  }

  private sealed class KafkaMessagingManifestValidationHostedService : IHostedService
  {
    private readonly IServiceCollection _services;

    public KafkaMessagingManifestValidationHostedService(IServiceCollection services)
        => _services = services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
      var registeredHandlers = GetRegisteredIntegrationEventHandlers(_services);
      var manifests = MessagingManifestRegistry.GetAll();
      var manifestSubscriptions = GetManifestSubscriptions();

      if (manifests.Count == 0 && registeredHandlers.Count > 0)
        throw new InvalidOperationException(
            "Integration event handlers are registered but no generated messaging manifest is available. Ensure the Franz mediator source generator runs in every handler assembly.");

      ValidateManifestSubscriptions(registeredHandlers, manifestSubscriptions);
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  }

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