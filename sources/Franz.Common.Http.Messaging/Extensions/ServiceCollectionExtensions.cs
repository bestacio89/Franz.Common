using System;
using Microsoft.Extensions.DependencyInjection;
using Confluent.Kafka;
using Franz.Common.Http.Messaging.Transactions;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddMessagingInHttpContext(this IServiceCollection services, IConfiguration configuration)
    {
      services
          .AddMessagingPublisher(configuration)
          .AddMessagingSender(configuration)
          .AddMessagingConsumer(configuration)
          .AddMessagingTransactionPerHttpCall()
          .AddMessagingHealthCheck(); // Register health check

      return services;
    }

    public static IServiceCollection AddMessagingTransactionPerHttpCall(this IServiceCollection services)
    {
      services
          .AddNoDuplicateScoped<TransactionFilter>()
          .AddMvc()
          .AddMvcOptions(setup =>
          {
            setup.Filters.AddService<TransactionFilter>();
          });

      return services;
    }

    public static IServiceCollection AddMessagingHealthCheck(this IServiceCollection services)
    {
      // Configure Kafka consumer properties
      var consumerConfig = new ConsumerConfig
      {
        // Your consumer configuration settings
      };

      services.AddSingleton(consumerConfig); // Register the consumer config

      // Register the health check
      services.AddHealthChecks()
              .AddCheck<KafkaHealthCheck>("Kafka", tags: new[] { "messaging" });

      return services;
    }
  }
}
