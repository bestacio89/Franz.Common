using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Http.Messaging.Transactions;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using HealthChecks.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Franz.Common.Http.Messaging.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingInHttpContext(this IServiceCollection services, IConfiguration configuration)
  {
    services
          .AddRabbitMQMessagingPublisher(configuration)
          .AddRabbitMQMessagingSender(configuration)
          .AddRabbitMQMessagingConsumer(configuration)
          .AddMessagingTransactionPerHttpCall()
          .AddMessagingHealthCheck();

    return services;
  }

  public static IServiceCollection AddMessagingTransactionPerHttpCall(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<MessagingTransactionFilter>()
      .AddMessagingHealthCheck()
      .AddMvc()
      .AddMvcOptions(setup =>
      {
        setup.Filters.AddService<MessagingTransactionFilter>();
      });

    return services;
  }

  public static IServiceCollection AddMessagingHealthCheck(this IServiceCollection services)
  {
    var rabbitMQHealthCheckType = typeof(RabbitMQHealthCheck);

    var allReadyReferenced = services.Any(service => service.ServiceType == rabbitMQHealthCheckType);

    if (!allReadyReferenced)
    {
      services
        .AddHealthChecks()
        .AddRabbitMQ(serviceProvider => serviceProvider.GetRequiredService<IConnectionProvider>().Current);
    }

    return services;
  }
}

