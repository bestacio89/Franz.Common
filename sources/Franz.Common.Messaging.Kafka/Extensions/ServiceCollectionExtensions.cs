using Microsoft.Extensions.Configuration;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Contexting;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Hosting;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Transactions;


namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddMessagingPublisher(configuration)
      .AddMessagingSender(configuration)
      .AddMessagingConsumer(configuration);

    return services;
  }

  public static IServiceCollection AddMessagingSender(this IServiceCollection services, IConfiguration? configuration = null)
  {
    services
      .AddNoDuplicateScoped<IMessagingSender, MessagingSender>()
      .AddCommonMessagingProducer(configuration);

    return services;
  }

  public static IServiceCollection AddMessagingPublisher(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddNoDuplicateScoped<IMessagingPublisher, MessagingPublisher>()
      .AddCommonMessagingProducer(configuration);

    return services;
  }

  private static IServiceCollection AddCommonMessagingProducer(this IServiceCollection services, IConfiguration? configuration)
  {
    services
          .AddMessagingConfiguration(configuration)
          .AddNoDuplicateScoped<IMessagingTransaction, MessagingTransaction>()
          .AddNoDuplicateScoped<IMessageFactory, MessageFactory>()
          .AddNoDuplicateScoped<IMessageHandler, MessageBuilderDelegatingHandler>();

    return services;
  }

  public static IServiceCollection AddMessagingConfiguration(this IServiceCollection services, IConfiguration? configuration)
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

  public static IServiceCollection AddMessagingConsumer(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddOnlyHighLifetimeModelProvider(ServiceLifetime.Singleton)
      .AddMessagingConfiguration(configuration)
      .AddNoDuplicateScoped<MessageContextAccessor>()
      .AddNoDuplicateScoped<IMessageContextAccessor>(sp => sp.GetRequiredService<MessageContextAccessor>())
      .AddNoDuplicateSingleton<IListener, KafkaListener>()
      .AddNoDuplicateSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();

    return services;
  }
}
