namespace Franz.Common.Messaging.Hosting.Azure.EventGrid;

public sealed class AzureEventGridEndpointOptions
{
  public string Route { get; set; } = "/events/eventgrid";
}
