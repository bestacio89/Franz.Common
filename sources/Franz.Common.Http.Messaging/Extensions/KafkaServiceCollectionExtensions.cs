using System;
using Microsoft.Extensions.DependencyInjection;
using Confluent.Kafka;
using Franz.Common.Http.Messaging.Transactions;
using Microsoft.Extensions.Configuration;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging.Kafka.Extensions;


  public static class KafkaServiceCollectionExtensions
  {
    public static IServiceCollection AddKafkaMessagingInHttpContext(this IServiceCollection services, IConfiguration configuration)
    {
      services
          .AddKafkaMessagingPublisher(configuration)
          .AddKafkaMessagingSender(configuration)
          .AddKafkaMessagingConsumer(configuration)
          .AddKafkaMessagingTransactionPerHttpCall()
          .AddKafkaMessagingHealthCheck(); // Register health check

      return services;
    }

    public static IServiceCollection AddKafkaMessagingTransactionPerHttpCall(this IServiceCollection services)
    {
      services
          .AddNoDuplicateScoped<MessagingTransactionFilter>()
          .AddMvc()
          .AddMvcOptions(setup =>
          {
            setup.Filters.AddService<MessagingTransactionFilter>();
          });

      return services;
    }

    public static IServiceCollection AddKafkaMessagingHealthCheck(this IServiceCollection services)
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
