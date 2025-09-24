using Franz.Common.HostingMessaging.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.HostingMessaging.Listeners;

public static class MessagingListenerExtensions
{
  /// <summary>
  /// Registers only the Kafka message listener.
  /// </summary>
  public static IServiceCollection AddKafkaListener(this IServiceCollection services)
  {
    services.AddHostedService<KafkaMessageListener>();
    return services;
  }

  /// <summary>
  /// Registers only the Outbox message listener.
  /// </summary>
  public static IServiceCollection AddOutboxListener(this IServiceCollection services)
  {
    services.AddHostedService<OutboxMessageListener>();
    return services;
  }
}
