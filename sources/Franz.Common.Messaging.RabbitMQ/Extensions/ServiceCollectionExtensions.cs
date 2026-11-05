using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Manifest;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Franz.Common.Messaging.RabbitMQ.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services, IConfiguration configuration)
  {
    services
        .AddRabbitMQMessagingPublisher(configuration)
        .AddRabbitMQMessagingSender(configuration)
        .AddRabbitMQMessagingConsumer(configuration);

    return services;
  }

  public static IServiceCollection AddRabbitMQMessagingSender(this IServiceCollection services, IConfiguration? configuration = null)
  {
    services
        .AddNoDuplicateScoped<IMessagingSender, RabbitMQMessagingSender>()
        .AddRabbitMQMessagingProducer(configuration);

    return services;
  }

  public static IServiceCollection AddRabbitMQMessagingPublisher(this IServiceCollection services, IConfiguration configuration)
  {
    services
        .AddNoDuplicateScoped<IMessagingPublisher, RabbitMQMessagingPublisher>()
        .AddRabbitMQMessagingProducer(configuration);
       
    return services;
  }

  private static IServiceCollection AddRabbitMQMessagingProducer(this IServiceCollection services, IConfiguration? configuration)
  {
    if (configuration is null)
      throw new ArgumentNullException(nameof(configuration));

    services
        .AddRabbitMQMessagingConfiguration(configuration)
        .AddNoDuplicateScoped<IMessageHandler, MessageBuilderDelegatingHandler>();

    return services;
  }

  public static IServiceCollection AddRabbitMqMessagingOptions(this IServiceCollection services, IConfiguration configuration)
  {
    var section = configuration.GetSection("Messaging:RabbitMQ");
    if (!section.Exists())
      throw new TechnicalException("RabbitMQ messaging configuration missing");

    var provisioningModeIsConfigured = section["ProvisioningMode"] is not null;

    services.AddOptions<RabbitMQMessagingOptions>()
        .Bind(section)
        .Validate(options =>
            !string.IsNullOrWhiteSpace(options.BootStrapServers) ||
            (!string.IsNullOrWhiteSpace(options.HostName) &&
             !string.IsNullOrWhiteSpace(options.UserName) &&
             !string.IsNullOrWhiteSpace(options.Password)),
            "Configure Messaging:RabbitMQ:BootStrapServers with an amqp(s) URI, or configure HostName, UserName, and Password explicitly.")
        .Validate(options =>
            string.IsNullOrWhiteSpace(options.BootStrapServers) ||
            (Uri.TryCreate(options.BootStrapServers, UriKind.Absolute, out var uri) &&
             (uri.Scheme == "amqp" || uri.Scheme == "amqps")),
            "Messaging:RabbitMQ:BootStrapServers must be an absolute amqp:// or amqps:// URI.")
        .Validate(options =>
            string.IsNullOrWhiteSpace(options.BootStrapServers) ||
            (Uri.TryCreate(options.BootStrapServers, UriKind.Absolute, out var bootstrapUri) &&
             !string.IsNullOrWhiteSpace(bootstrapUri.Host) &&
             bootstrapUri.UserInfo.Contains(":", StringComparison.Ordinal) &&
             bootstrapUri.UserInfo.Split(':', 2)[0].Length > 0 &&
             bootstrapUri.UserInfo.Split(':', 2)[1].Length > 0),
            "Messaging:RabbitMQ:BootStrapServers must include a host and explicit username/password credentials.")
        .Validate(options =>
            string.IsNullOrWhiteSpace(options.BootStrapServers) ||
            options.SslEnabled != true ||
            (Uri.TryCreate(options.BootStrapServers, UriKind.Absolute, out var secureUri) &&
             secureUri.Scheme == "amqps"),
            "RabbitMQ TLS is enabled but Messaging:RabbitMQ:BootStrapServers does not use amqps://.")
        .Validate(options =>
            options.RequestedHeartbeatSeconds is null or > 0,
            "Messaging:RabbitMQ:RequestedHeartbeatSeconds must be greater than zero when configured.")
        .Validate(options =>
            Enum.IsDefined(options.ReplayStrategy),
            "Messaging:RabbitMQ:ReplayStrategy must be a defined RabbitMQReplayStrategy value.")
        .Validate(options =>
            options.PublisherConfirmTimeoutSeconds > 0,
            "Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds must be greater than zero.")
        .Validate(options =>
            options.MaxConcurrency > 0 && options.MaxConcurrency <= ushort.MaxValue,
            "Messaging:RabbitMQ:MaxConcurrency must be between 1 and 65535.")
        .Validate(options => Enum.IsDefined(options.QueueType),
            "Messaging:RabbitMQ:QueueType must be Quorum or Classic.")
        .Validate(options => options.MessageTtlMilliseconds is null or > 0,
            "Messaging:RabbitMQ:MessageTtlMilliseconds must be greater than zero when configured.")
        .Validate(options => options.MaxLength is null or > 0,
            "Messaging:RabbitMQ:MaxLength must be greater than zero when configured.")
        .Validate(options => options.MaxLengthBytes is null or > 0,
            "Messaging:RabbitMQ:MaxLengthBytes must be greater than zero when configured.")
        .Validate(options => options.DeliveryLimit is null or > 0,
            "Messaging:RabbitMQ:DeliveryLimit must be greater than zero when configured.")
        .Validate(options => options.DeliveryLimit is null || options.QueueType == RabbitMQQueueType.Quorum,
            "Messaging:RabbitMQ:DeliveryLimit is supported only for quorum queues.")
        .Validate(options => options.OrderingStrategy is null || Enum.IsDefined(options.OrderingStrategy.Value),
            "Messaging:RabbitMQ:OrderingStrategy must be a defined MessageOrderingStrategy value.")
        .Validate(options => options.OrderingStrategy is null or MessageOrderingStrategy.None || options.MaxConcurrency == 1,
            "RabbitMQ preserves configured ordering only when MaxConcurrency is 1.")
        .Validate(options => options.FailurePolicy is null ||
            options.FailurePolicy.MaxRetryAttempts == 0 ||
            options.ReplayStrategy == RabbitMQReplayStrategy.ProgressiveFourStep,
            "RabbitMQ FailurePolicy retries require ReplayStrategy=ProgressiveFourStep.")
        .ValidateOnStart();

    services.AddSingleton<IConfigureOptions<RabbitMQMessagingOptions>>(serviceProvider =>
        new ConfigureNamedOptions<RabbitMQMessagingOptions>(Options.DefaultName, options =>
        {
          if (provisioningModeIsConfigured)
            return;

          var environment = serviceProvider.GetService<IHostEnvironment>();
          options.ProvisioningMode = environment?.IsDevelopment() == true
              ? ProvisioningMode.Create
              : ProvisioningMode.Verify;
        }));

    return services;
  }

  public static IServiceCollection AddRabbitMQMessagingConfiguration(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    if (configuration is null)
      throw new ArgumentNullException(nameof(configuration));

    services
        .AddRabbitMqMessagingOptions(configuration)
        .AddNoDuplicateSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>()
        .AddNoDuplicateSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>()
        .AddNoDuplicateSingleton<IConnectionProvider, ConnectionProvider>()
        .AddNoDuplicateSingleton<IChannelPool, ChannelPool>()
        .AddNoDuplicateSingleton<IConfirmChannelPool>(sp =>
            (IConfirmChannelPool)sp.GetRequiredService<IChannelPool>())
        .AddMessagingFactories()
        .AddNoDuplicateScoped<IMessagingInitializer, RabbitMQMessagingInitializer>()
        .AddNoDuplicateScoped<IMessagingTransaction, RabbitMQMessagingTransaction>();

    return services;
  }

  private static void ValidateMessagingManifest(IServiceCollection services)
  {
    var registeredHandlers = services
        .Where(static descriptor => descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition().Name.StartsWith("INotificationHandler", StringComparison.Ordinal))
        .Select(static descriptor => descriptor.ServiceType.GenericTypeArguments[0])
        .Where(static type => typeof(IIntegrationEvent).IsAssignableFrom(type)).ToHashSet();

    var manifests = MessagingManifestRegistry.GetAll();
    if (manifests.Count == 0 && registeredHandlers.Count > 0)
      throw new InvalidOperationException(
          "Integration event handlers are registered but no generated messaging manifest is available. Ensure the Franz mediator source generator runs in every handler assembly.");

    var manifestSubscriptions = manifests.SelectMany(static manifest => manifest.Subscriptions)
        .Where(static type => typeof(IIntegrationEvent).IsAssignableFrom(type)).ToHashSet();
    var missingFromManifest = registeredHandlers.Except(manifestSubscriptions).ToArray();
    var missingHandlers = manifestSubscriptions.Except(registeredHandlers).ToArray();
    if (missingFromManifest.Length > 0 || missingHandlers.Length > 0)
      throw new InvalidOperationException(
          $"Generated messaging manifest and registered integration event handlers disagree. Missing from manifest: {string.Join(", ", missingFromManifest.Select(static type => type.FullName))}. Missing registered handlers: {string.Join(", ", missingHandlers.Select(static type => type.FullName))}.");
  }

  private sealed class RabbitMQMessagingManifestValidationHostedService : IHostedService
  {
    private readonly IServiceCollection _services;

    public RabbitMQMessagingManifestValidationHostedService(IServiceCollection services)
        => _services = services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
      ValidateMessagingManifest(_services);
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  }

  public static IServiceCollection AddOnlyHighLifetimeModelProvider(this IServiceCollection services, ServiceLifetime serviceLifetime)
  {
    var serviceType = typeof(IModelProvider);
    var descriptor = new ServiceDescriptor(serviceType, typeof(ModelProvider), serviceLifetime);

    var existing = services.SingleOrDefault(s => s.ServiceType == serviceType);
    if (existing != null)
    {
      // Upgrade lifetime if needed
      if (existing.Lifetime != serviceLifetime && serviceLifetime == ServiceLifetime.Singleton)
      {
        services.Remove(existing);
        services.Add(descriptor);
      }
    }
    else
    {
      services.Add(descriptor);
    }

    return services;
  }

  public static IServiceCollection AddRabbitMQMessagingConsumer(this IServiceCollection services, IConfiguration configuration)
  {
    services.TryAddSingleton<IServiceCollection>(services);
    services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RabbitMQMessagingManifestValidationHostedService>());
    services.AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton)
            .AddRabbitMQMessagingConfiguration(configuration)
            .AddNoDuplicateScoped<MessageContextAccessor>()
            .AddNoDuplicateScoped<IMessageContextAccessor>(sp => sp.GetRequiredService<MessageContextAccessor>())
            .AddNoDuplicateSingleton<IBasicConsumerFactory, BasicConsumerFactory>()
            .AddSingleton<IReplayStrategy>(sp =>
            {
              var options = sp.GetRequiredService<IOptions<RabbitMQMessagingOptions>>().Value;
              var confirmChannelPool = sp.GetRequiredService<IConfirmChannelPool>();
              var accessor = sp.GetRequiredService<IAssemblyAccessor>();
              var rabbitOptions = sp.GetRequiredService<IOptions<RabbitMQMessagingOptions>>();

              return options.ReplayStrategy == RabbitMQReplayStrategy.ProgressiveFourStep
                  ? new ProgressiveFourStepReplayStrategy(confirmChannelPool, accessor, rabbitOptions)
                  : new NoReplayStrategy(confirmChannelPool, accessor, rabbitOptions);
            });

    return services;
  }
}