using Franz.Common.Mapping.Extensions;
using Franz.Common.Messaging.AzureEventGrid.Configuration;
using Franz.Common.Messaging.AzureEventGrid.Ingress;
using Franz.Common.Messaging.AzureEventGrid.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.AzureEventGrid;

public static class AzureEventGridServiceCollectionExtensions
{
  public static IServiceCollection AddFranzAzureEventGrid(
    this IServiceCollection services)
  {
    

    services.AddSingleton<AzureEventGridMessageMapper>();
    services.AddSingleton<IAzureEventGridIngress, AzureEventGridIngress>();
    services.AddSingleton<AzureEventGridFilterOptions>();

    return services;
  }
}
