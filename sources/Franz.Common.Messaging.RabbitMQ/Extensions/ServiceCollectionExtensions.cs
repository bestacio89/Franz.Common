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

    services.AddOptions<RabbitMQMessagingOptions>().Bind(section);
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
            .AddSingleton<IReplayStrategy, NoReplayStrategy>();

    return services;
  }
}