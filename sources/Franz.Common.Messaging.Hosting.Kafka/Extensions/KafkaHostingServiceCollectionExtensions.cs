using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Hosting.Listeners;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Hosting.Kafka;

public static class KafkaHostingServiceCollectionExtensions
{
  public static IServiceCollection AddKafkaHostedListener(
      this IServiceCollection services,
      Action<KafkaMessagingOptions> configureOptions)
  {
    services.Configure(configureOptions);

    // Register one listener instance for both direct resolution and hosted execution.
    services.AddSingleton<KafkaMessageListener>();
    services.AddSingleton<IListener>(serviceProvider =>
        serviceProvider.GetRequiredService<KafkaMessageListener>());

    services.AddHostedService<KafkaMessagingHostedService>();

    return services;
  }

  public static IServiceCollection AddOutboxHostedListener(
      this IServiceCollection services,
      Action<OutboxOptions> configureOptions)
  {
    services.Configure(configureOptions);

    services.AddSingleton<OutboxMessageListener>();
    services.AddHostedService<OutboxHostedService>();

    return services;
  }
}
