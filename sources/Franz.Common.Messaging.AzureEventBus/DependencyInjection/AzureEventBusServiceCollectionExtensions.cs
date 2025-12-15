using Azure.Messaging.ServiceBus;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventBus.Configuration;
using Franz.Common.Messaging.AzureEventBus.Consumers;
using Franz.Common.Messaging.AzureEventBus.Infrastructure;
using Franz.Common.Messaging.AzureEventBus.Mapping;
using Franz.Common.Messaging.AzureEventBus.Producers;
using Franz.Common.Mapping.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Franz.Common.Messaging.AzureEventBus.Hosting;

namespace Franz.Common.Messaging.AzureEventBus.DependencyInjection;

public static class AzureEventBusServiceCollectionExtensions
{
  public static IServiceCollection AddFranzAzureEventBus(
      this IServiceCollection services,
      Action<AzureEventBusOptions> configure)
  {
    // ─────────────────────────────────────────────────────────────
    // Options
    // ─────────────────────────────────────────────────────────────
    var options = new AzureEventBusOptions();
    configure(options);
    options.Validate();

    services.AddSingleton(options);

    // ─────────────────────────────────────────────────────────────
    // Azure SDK infrastructure
    // ─────────────────────────────────────────────────────────────
    services.AddSingleton<ServiceBusClientFactory>();
    services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClientFactory>().Create());

    services.AddSingleton<ServiceBusSenderFactory>();
    services.AddSingleton<ServiceBusProducerFactory>();

    // ─────────────────────────────────────────────────────────────
    // Franz mapping (transport payload ↔ Message)
    // ─────────────────────────────────────────────────────────────
    services.AddFranzMapping(
        assemblies: new[] { typeof(AzureEventBusMapProfile).Assembly });

    services.AddSingleton<IAzureEventBusMessageMapper, AzureEventBusMessageMapper>();

    // ─────────────────────────────────────────────────────────────
    // Producer (IMessagingSender)
    // ─────────────────────────────────────────────────────────────
    services.AddSingleton<IMessagingSender, AzureEventBusProducer>();

    // ─────────────────────────────────────────────────────────────
    // Consumers / processors
    // ─────────────────────────────────────────────────────────────
    services.AddSingleton<AzureEventBusProcessor>(sp =>
    {
      var client = sp.GetRequiredService<ServiceBusClient>();
      var mapper = sp.GetRequiredService<IAzureEventBusMessageMapper>();
      var dispatcher = sp.GetRequiredService<IDispatcher>();
      var logger = sp.GetRequiredService<ILogger<AzureEventBusProcessor>>();
      var options = sp.GetRequiredService<AzureEventBusOptions>();

      return new AzureEventBusProcessor(
          client,
          options.EntityName,
          mapper,
          dispatcher,
          logger,
          options.ToProcessorOptions());
    });

    services.AddSingleton<AzureEventBusConsumer>();

    // ─────────────────────────────────────────────────────────────
    // Hosting
    // ─────────────────────────────────────────────────────────────
    services.AddHostedService<AzureEventBusHostedService>();

    return services;
  }
}
