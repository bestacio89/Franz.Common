using Confluent.Kafka;
using Franz.Common.Messaging.MassTransit.Contracts;
using Franz.Common.Messaging.MassTransit;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public static class MassTransitKafkaServiceRegistration
{
  public static void AddMassTransitKafkaServices(
      this IServiceCollection services,
      string kafkaBootstrapServers,
      Action<IRiderRegistrationConfigurator> configureConsumers
  )
  {
    services.AddMassTransit(x =>
    {
      // In-memory bus (optional, for testing or local)
      x.UsingInMemory((context, cfg) =>
      {
        cfg.ConfigureEndpoints(context);
      });

      // Kafka rider
      x.AddRider(rider =>
      {
        configureConsumers?.Invoke(rider);

        rider.UsingKafka((context, kafka) =>
        {
          kafka.Host(kafkaBootstrapServers);

          // Additional Kafka-specific configuration can go here
        });
      });
    });

    // Register the Kafka producer as a singleton
    services.AddSingleton<IProducer<string, string>>(provider =>
    {
      var config = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
      return new ProducerBuilder<string, string>(config).Build();
    });

    // Register your KafkaProducer
    services.AddScoped<IKafkaProducer, KafkaProducer>();

  }
}
