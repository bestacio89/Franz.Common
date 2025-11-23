using Franz.Common.Messaging;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


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
      .AddNoDuplicateScoped<IMessagingSender, MessagingSender>()
      .AddRabbitMQMessagingProducer(configuration);

    return services;
  }

  public static IServiceCollection AddRabbitMQMessagingPublisher(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddNoDuplicateScoped<IMessagingPublisher, MessagingPublisher>()
      .AddRabbitMQMessagingProducer(configuration);

    return services;
  }

  private static IServiceCollection AddRabbitMQMessagingProducer(this IServiceCollection services, IConfiguration? configuration)
  {
    services
          .AddRabbitMQMessagingConfiguration(configuration)
          
          .AddNoDuplicateScoped<IMessageFactory, MessageFactory>()
          .AddNoDuplicateScoped<IMessageHandler, MessageBuilderDelegatingHandler>();

    return services;
  }

  public static IServiceCollection AddRabbitMQMessagingConfiguration(this IServiceCollection services, IConfiguration? configuration)
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

  public static IServiceCollection AddRabbitMQMessagingConsumer(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton)
      .AddRabbitMQMessagingConfiguration(configuration)
      .AddNoDuplicateScoped<MessageContextAccessor>()
      .AddNoDuplicateScoped<IMessageContextAccessor>(sp => sp.GetRequiredService<MessageContextAccessor>())
      .AddNoDuplicateSingleton<IListener, Listener>()
      .AddNoDuplicateSingleton<IBasicConsumerFactory, BasicConsumerFactory>()
      .AddSingleton<IReplayStrategy, NoReplayStrategy>();

    return services;
  }
}
