using Franz.Common.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Outboxes;


public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddOutboxPublisher(
      this IServiceCollection services,
      Action<OutboxOptions>? configure = null)
  {
    var options = new OutboxOptions();
    configure?.Invoke(options);

    services.AddSingleton(options);
    services.AddHostedService<OutboxPublisherService>();

    return services;
  }
}

