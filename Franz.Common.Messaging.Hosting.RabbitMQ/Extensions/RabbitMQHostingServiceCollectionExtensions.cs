using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Outbox;
using Franz.Common.Messaging.Hosting.Listeners;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using System.Runtime.CompilerServices;

namespace Franz.Common.Messaging.Hosting.RabbitMQ;

public static class RabbitMQHostingServiceCollectionExtensions
{
  public static IServiceCollection AddRabbitMQHostedListener(
      this IServiceCollection services,
      Action<MessagingOptions> configureOptions)
  {
    services.Configure(configureOptions);
 
    // Transport-level Kafka listener
    services.AddSingleton<Listener>();

    // Hosted service wrapping the listener
    services.AddHostedService<RabbitMQHostedService>();

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
