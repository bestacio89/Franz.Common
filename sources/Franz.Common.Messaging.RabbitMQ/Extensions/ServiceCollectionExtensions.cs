using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Franz.Common.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            string.Equals(options.ReplayStrategy, "NoReplay", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.ReplayStrategy, "ProgressiveFourStep", StringComparison.OrdinalIgnoreCase),
            "Messaging:RabbitMQ:ReplayStrategy must be NoReplay or ProgressiveFourStep.")
        .Validate(options =>
            options.PublisherConfirmTimeoutSeconds > 0,
            "Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds must be greater than zero.")
        .Validate(options =>
            options.MaxConcurrency > 0 && options.MaxConcurrency <= ushort.MaxValue,
            "Messaging:RabbitMQ:MaxConcurrency must be between 1 and 65535.")
        .ValidateOnStart();

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
              var messagingOptions = sp.GetRequiredService<IOptions<MessagingOptions>>();

              return string.Equals(options.ReplayStrategy, "ProgressiveFourStep", StringComparison.OrdinalIgnoreCase)
                  ? new ProgressiveFourStepReplayStrategy(confirmChannelPool, accessor, messagingOptions)
                  : new NoReplayStrategy(confirmChannelPool, accessor, messagingOptions);
            });

    return services;
  }
}