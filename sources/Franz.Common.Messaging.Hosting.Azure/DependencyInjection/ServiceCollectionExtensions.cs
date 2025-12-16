using Franz.Common.Messaging.AzureEventBus.DependencyInjection;
using Franz.Common.Messaging.AzureEventGrid;
using Franz.Common.Messaging.AzureEventHubs.DependencyInjection;
using Franz.Common.Messaging.Hosting.Azure.EventBus;
using Franz.Common.Messaging.Hosting.Azure.EventHubs;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Hosting.Azure.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddFranzAzureHosting(
      this IServiceCollection services,
      Action<FranzAzureHostingOptions> configure)
  {
    var options = new FranzAzureHostingOptions();
    configure(options);
    options.Validate();

    // Transport registrations (each package owns its internals)
    services.AddFranzAzureEventBus(options.EventBus);
    services.AddFranzAzureEventHubs(options.EventHubs);
    services.AddFranzAzureEventGrid();

    // Hosted services (background listeners)
    services.AddHostedService<AzureEventBusHostedService>();
    services.AddHostedService<AzureEventHubsHostedService>();

    return services;
  }
}
