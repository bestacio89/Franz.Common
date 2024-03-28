using HealthChecks.AzureServiceBus;
using Franz.Common.Http.Messaging.Transactions;
using Franz.Common.Messaging.AzureEventBus.Connections;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingInHttpContext(this IServiceCollection services, IConfiguration configuration)
    {
        services
              .AddMessagingPublisher(configuration)
              .AddMessagingSender(configuration)
              .AddMessagingConsumer(configuration)
              .AddMessagingTransactionPerHttpCall()
              .AddMessagingHealthCheck();

        return services;
    }

    public static IServiceCollection AddMessagingTransactionPerHttpCall(this IServiceCollection services)
    {
        services
          .AddNoDuplicateScoped<TransactionFilter>()
          .AddMessagingHealthCheck()
          .AddMvc()
          .AddMvcOptions(setup =>
          {
              setup.Filters.AddService<TransactionFilter>();
          });

        return services;
    }

    public static IServiceCollection AddMessagingHealthCheck(this IServiceCollection services)
    {
        var AzureEventBusHealthCheckType = typeof(AzureServiceBusHealthCheck<>);

        var allReadyReferenced = services.Any(service => service.ServiceType == AzureEventBusHealthCheckType);

        if (!allReadyReferenced)
        {
              services.AddHealthChecks();
             
        }

        return services;
    }
}
