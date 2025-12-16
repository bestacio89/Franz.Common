using Franz.Common.Messaging.AzureEventHubs.Configuration;
using Franz.Common.Messaging.AzureEventHubs.Consumers;
using Franz.Common.Messaging.AzureEventHubs.Infrastructure;
using Franz.Common.Messaging.AzureEventHubs.Mapping;
using Franz.Common.Messaging.AzureEventHubs.Producers;
using Franz.Common.Messaging.AzureEventHubs.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.AzureEventHubs.DependencyInjection;

public static class AzureEventHubsServiceCollectionExtensions
{
  public static IServiceCollection AddFranzAzureEventHubs(
      this IServiceCollection services,
      Action<AzureEventHubsOptions> configure)
  {
    var options = new AzureEventHubsOptions();
    configure(options);
    options.Validate();

    services.AddSingleton(options);

    services.AddSingleton<EventHubsClientFactory>();
    services.AddSingleton<EventHubsProcessorFactory>();

    services.AddSingleton<AzureEventHubsMessageSerializer>();
    services.AddSingleton<AzureEventHubsMessageMapper>();

    services.AddSingleton<AzureEventHubsProducer>();
    services.AddSingleton<AzureEventHubsProcessor>();

    return services;
  }
}
