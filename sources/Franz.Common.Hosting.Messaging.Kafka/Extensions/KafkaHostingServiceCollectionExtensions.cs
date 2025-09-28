using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.Hosting.Listeners;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Hosting.Kafka;

public static class KafkaHostingServiceCollectionExtensions
{
  public static IServiceCollection AddKafkaHostedListener(
      this IServiceCollection services,
      Action<MessagingOptions> configureOptions)
  {
    services.Configure(configureOptions);

    // Transport-level Kafka listener
    services.AddSingleton<KafkaMessageListener>();

    // Hosted service wrapping the listener
    services.AddHostedService<KafkaHostedService>();

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
