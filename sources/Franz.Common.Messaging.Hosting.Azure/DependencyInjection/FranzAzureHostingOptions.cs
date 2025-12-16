using Franz.Common.Errors;
using Franz.Common.Messaging.AzureEventBus.Configuration;
using Franz.Common.Messaging.AzureEventGrid.Configuration;
using Franz.Common.Messaging.AzureEventHubs.Configuration;
using Franz.Common.Messaging.Hosting.Azure.EventGrid;

namespace Franz.Common.Messaging.Hosting.Azure.DependencyInjection;

public sealed class FranzAzureHostingOptions
{
  public Action<AzureEventBusOptions> EventBus { get; set; } = _ => { };
  public Action<AzureEventHubsOptions> EventHubs { get; set; } = _ => { };
  public Action<AzureEventGridEndpointOptions> EventGrid { get; set; } = _ => { };

  internal void Validate()
  {
    if (EventBus is null) throw new TechnicalException("EventBus configuration is required.");
    if (EventHubs is null) throw new TechnicalException("EventHubs configuration is required.");
    if (EventGrid is null) throw new TechnicalException("EventGrid configuration is required.");
  }
}
